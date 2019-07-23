using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public delegate void NetMes(byte[] mes);

public class Network
{
    public ConcurrentQueue<byte[]> recvMes = new ConcurrentQueue<byte[]>();   //上次接收到的信息，读取完应将此变量设为null才能接收下条信息

    UdpClient udpClient;
    IPEndPoint recvPoint = new IPEndPoint(IPAddress.Any, 0);
    IPEndPoint serverPoint; //服务器信息
    Thread recvThread;

    private Dictionary<int, NetMes> callbacks = new Dictionary<int, NetMes>();

    /*以下为常量 */
    public const int DataStartIndex = 4;    //每个数据包的数据起始位置，0-3字节为int型表示了何种请求
    //登陆
    public const int LOGIN = 0;
    public const int NO_NET = 0;  //登陆或注册无网络
    public const int LOGIN_INPUT_ERROR = 1; //用户名或密码错误
    //注册
    public const int REGISTER = 1;
    public const int REGISTER_RENAME = 1;   //重名
    //对局开始
    public const int RIVAL_START = 3;
    //敌人信息上传
    public const int RIVAL_NUM = 4;

    public const int RIVAL_REQUEST = 2; //敌人生成与控制的网络请求
    public const int RIVAL_CREATE = 0;  //敌人生成
    public const int PLAYER_IN_SIGHT = 1;   //在敌人视线内
    public const int TRACE = 2; //追踪玩家
    public const int PLAYER_IN_SHOOT = 3;   //玩家在射程内
    public const int PLAYER_OUT_SHOOT = 4;  //玩家从射程内到射程外
    public const int SHOOT = 5;
    public const int SEARCH_ROAD = 6;   //寻路

    //对局信息上传
    public const int MATCH_INFO = 5;    //对局信息的上传
    public const int ACK = 6;   //接受确认

    
    private int clientPort;

    public Network(int clientPort, string serverIp, int serverPort)
    {
        serverPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

        this.clientPort = clientPort;
        udpClient = new UdpClient(clientPort);
        recvThread = new Thread(RecvMes);
        recvThread.Start();
    }

    public void Close()
    {
        if(recvThread.IsAlive) recvThread.Abort();
        udpClient.Close();
    }

    private void RecvMes()
    {
        while(true)
        {
            try{
                recvMes.Enqueue(udpClient.Receive(ref recvPoint));
            }
            catch(SocketException e)
            {
                udpClient.Close();
                udpClient = new UdpClient(clientPort);
            }
            // Debug.Log(System.Text.Encoding.UTF8.GetString(recvMes));
            // Debug.Log(System.BitConverter.ToDouble(recvMes, 0));
            // Debug.Log(System.BitConverter.ToInt32(recvMes, sizeof(double)));
            // Debug.Log(System.BitConverter.ToUInt32(Global.GetSub(recvMes, sizeof(double)+sizeof(int), sizeof(double)+sizeof(int)+sizeof(uint)), 0));
            // recvMes = null;
            // int mesCode = System.BitConverter.ToInt32(recvMes, 0);
            // if(callbacks.ContainsKey(mesCode))
            // {
            //     callbacks[mesCode]();
            // }
        }
    }

    public void Send(byte[] data)
    {
        udpClient.Send(data, data.Length, serverPoint);
    }

    public void DelegateNetMes(int code, NetMes callback)
    {
        if(callbacks.ContainsKey(code))
        {
            callbacks[code] += callback;
        }
        else
        {
            callbacks.Add(code, callback);
        }
    }

    public void ReleaseNetMes(int code, NetMes callback)
    {
        callbacks[code] -= callback;
    }

    public void CallWithMesCode(int code, byte[] mes)
    {
        if(callbacks.ContainsKey(code))
        {
            callbacks[code](mes);
        }
    }
}