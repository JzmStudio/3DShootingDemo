using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class StartController : MonoBehaviour
{
    public GameObject[] LogInObjs;  //登录界面的相关UI
    public GameObject[] PlayMenuObjs;   //开始游戏界面的相关UI

    public InputField userNameInput;
    public InputField passwordInput;
    public float outTime = 5f;  //网络超时时间
    public Text promptText;     //提示字

    private bool isLogIn = false;   //标记是否在已登录状态中
    private bool isInLogIn = false; //是否点击登录但还未收到服务器回复
    private bool isInRegister = false;  //是否点击注册但还未收到服务器回复

    Coroutine LogInOutCor;
    Coroutine RegisterOutCor;


    private void Awake()
    {
        // Global.network = new Network(888, "127.0.0.1", 999);    //初始化全局的网络模块
        Global.network.DelegateNetMes(Network.LOGIN, new NetMes(LogInRe));
        Global.network.DelegateNetMes(Network.REGISTER, new NetMes(RegisterRe));
    }

    // Start is called before the first frame update
    void Start()
    {
        if(isLogIn)
        {
            foreach(GameObject obj in LogInObjs) obj.SetActive(false);
            foreach(GameObject obj in PlayMenuObjs) obj.SetActive(true);
        }
        else
        {
            foreach(GameObject obj in LogInObjs) obj.SetActive(true);
            foreach(GameObject obj in PlayMenuObjs) obj.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*登录 */
    public void LogIn()
    {
        if(isInLogIn || isInRegister) return;   //若已点击了登陆按钮或注册按钮则不响应

        StopCoroutine("ResetPrompt");
        promptText.text = "";

        userNameInput.interactable = false;
        passwordInput.interactable = false;
        isInLogIn = true;
        //test
        // LogInSuccess();
        ByteBuilder builder = new ByteBuilder();
        builder.Add(System.BitConverter.GetBytes(Network.LOGIN));
        builder.Add(System.BitConverter.GetBytes(userNameInput.text.Length));
        builder.Add(System.BitConverter.GetBytes(passwordInput.text.Length));
        builder.Add(System.Text.Encoding.ASCII.GetBytes(userNameInput.text));
        builder.Add(System.Text.Encoding.ASCII.GetBytes(passwordInput.text));
        Global.network.Send(builder.GetByes());
        // Debug.Log("Set");
        LogInOutCor = StartCoroutine(LogInTimeout());
    }

    public void LogInRe(byte[] mes)
    {
        StopCoroutine(LogInOutCor);
        // Debug.Log("Recd"+System.BitConverter.ToInt32(mes, Network.DataStartIndex + 1));
        // Debug.Log("Recd"+System.BitConverter.ToInt64(mes, Network.DataStartIndex + 1));
        userNameInput.interactable = true;
        passwordInput.interactable = true;
        if(System.BitConverter.ToBoolean(mes, Network.DataStartIndex))  //登入
        {
            Global.killSum = System.BitConverter.ToInt32(mes, Network.DataStartIndex + 1);
            Global.maxAliveTime = (float)System.BitConverter.ToDouble(mes, Network.DataStartIndex + 1 + 4);
            Global.userName = userNameInput.text;
            LogInSuccess();
        }
        else
        {
            LogInFail(Network.LOGIN_INPUT_ERROR);
        }

        isInLogIn = false;
    }

    IEnumerator LogInTimeout()
    {
        yield return new WaitForSeconds(outTime);
        // Debug.Log("Timeout");
        isInLogIn = false;
        userNameInput.interactable = true;
        passwordInput.interactable = true;
        LogInFail(Network.NO_NET);
    }

    public void LogInFail(int errorCode)
    {
        switch(errorCode)
        {
            case Network.LOGIN_INPUT_ERROR:
                promptText.text = "用户名或密码错误";
                break;
            case Network.NO_NET:
                promptText.text = "网络请求超时";
                break;
            default:
                promptText.text = "";
                break;
        }
        userNameInput.interactable = true;
        passwordInput.interactable = true;
        StartCoroutine("ResetPrompt");
    }

    IEnumerator ResetPrompt()
    {
        yield return new WaitForSeconds(3.5f);
        promptText.text = "";
    }

    public void LogInSuccess()
    {
        foreach(GameObject obj in LogInObjs) obj.SetActive(false);
        foreach(GameObject obj in PlayMenuObjs) obj.SetActive(true);
    }

    /*注册 */
    public void Register()
    {
        if(isInLogIn || isInRegister) return;   //若已点击了登陆按钮或注册按钮则不响应
        userNameInput.interactable = false;
        passwordInput.interactable = false;

        StopCoroutine("ResetPrompt");
        promptText.text = "";
        isInRegister = true;

        ByteBuilder builder = new ByteBuilder();
        builder.Add(System.BitConverter.GetBytes(Network.REGISTER));
        builder.Add(System.BitConverter.GetBytes(userNameInput.text.Length));
        builder.Add(System.BitConverter.GetBytes(passwordInput.text.Length));
        builder.Add(System.Text.Encoding.ASCII.GetBytes(userNameInput.text));
        builder.Add(System.Text.Encoding.ASCII.GetBytes(passwordInput.text));
        Global.network.Send(builder.GetByes());

        RegisterOutCor = StartCoroutine(RegisterTimeout());
    }

    IEnumerator RegisterTimeout()
    {
        yield return new WaitForSeconds(outTime);
        isInRegister = false;
        userNameInput.interactable = true;
        passwordInput.interactable = true;
        RegisterFail(Network.NO_NET);
    }

    void RegisterRe(byte[] mes)
    {
        StopCoroutine(RegisterOutCor);
        userNameInput.interactable = true;
        passwordInput.interactable = true;
        if(System.BitConverter.ToBoolean(mes, Network.DataStartIndex))  //登入
        {
            RegisterSuccess();
        }
        else
        {
            RegisterFail(Network.REGISTER_RENAME);
        }

        isInRegister = false;
    }

    public void RegisterFail(int errorCode)
    {
        switch(errorCode)
        {
            case Network.REGISTER_RENAME:
                promptText.text = "用户名重复";
                break;
            case Network.NO_NET:
                promptText.text = "网络请求超时";
                break;
            default:
                promptText.text = "";
                break;
        }
        StartCoroutine("ResetPrompt");
    }

    public void RegisterSuccess()
    {
        Global.userName = userNameInput.text;
        foreach(GameObject obj in LogInObjs) obj.SetActive(false);
        foreach(GameObject obj in PlayMenuObjs) obj.SetActive(true);
    }

    public void BackToLogIn()
    {
        foreach(GameObject obj in LogInObjs) obj.SetActive(true);
        foreach(GameObject obj in PlayMenuObjs) obj.SetActive(false);
    }

    public void OnGameEsc()
    {
        Application.Quit();
    }
}
