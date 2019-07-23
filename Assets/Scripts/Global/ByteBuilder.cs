using System.Collections;
using System.Collections.Generic;

public class ByteBuilder
{
    private LinkedList<byte[]> list;

    public ByteBuilder()
    {
        list = new LinkedList<byte[]>();
    }

    public void Add(byte[] data)
    {
        list.AddLast(data);
    }

    public byte[] GetByes()
    {
        int len = 0;
        foreach(byte[] b in list)
        {
            len += b.Length;
        }
        byte[] res = new byte[len];
        int index = 0;
        foreach(byte[] b in list)
        {
            System.Buffer.BlockCopy(b, 0, res, index, b.Length);
            index += b.Length;
        }
        return res;
    }

    public void Clear()
    {
        list.Clear();
    }
}
