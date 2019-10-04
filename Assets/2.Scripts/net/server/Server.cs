﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Threading;
using System.Net;
using System.Text;
using System.IO;
using UnityEngine.UI;

public class Server : MonoBehaviour
{

    private List<ServerClient> clients;
    private TcpListener server;
    private bool serverStarted;


    private void Awake()
    {
        clients = new List<ServerClient>();
        
    }

    public bool ServerStart(int port)
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            StartListening();
            serverStarted = true;
            Debug.Log("Server has been started on port " + port.ToString());

            return true;
        }
        catch(Exception e)
        {
            Debug.Log("Socket error: " + e.Message);


            return false;
        }
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private void AcceptTcpClient(IAsyncResult ar) //Rock 걸어주기
    {
        TcpListener listener = (TcpListener)ar.AsyncState;

        ServerClient client = new ServerClient(listener.EndAcceptTcpClient(ar) , (byte)clients.Count);

  

        
        if (clients.Count > 4) //방이 꽉 찼을때
        {
            Send(client, new Packet(0, 5, Packet.Type.UNABLE_ACCES));//접속 불가란 신호를 보냄
            client.Close();
            return;
        }

        clients.Add(client);


        Thread thread = new Thread(() => ClientInComingPacket(client));
        thread.Start();

        StartListening();
    }

    private void ClientInComingPacket(ServerClient client)
    {
   

        while (IsConnected(client.Tcp))
        {
            if (client.IsDataAvailable())
            {
                Stream e = client.getStream();
                Packet packet;
                PacketParser.Pasing(e, out packet);
                if(packet != null)
                PacketManager.instance.putPacket(packet);

            }

        }

        clients.Remove(client);
        client.Close();
        


    }
    
    private bool IsConnected(TcpClient c)
    {
        try
        {
            if(c != null && c.Client != null && c.Client.Connected)
            {
                if(c.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {

            return false;
        }
    }
    


    void Update()
    {
        if (!serverStarted)
            return;

        //t.text = PacketManager.instance.receivePacket.Count.ToString();
    }

    private void Send(ServerClient client, Packet packet)
    {
        Stream stream = client.getStream();
        byte[] writerData = packet.Data;
        stream.Write(writerData, 0, writerData.Length);
        stream.Flush();
    }
    
}

public class ServerClient
{
    private TcpClient tcp;
    private string clientIP;
    private Stream stream;
    private NetworkStream ns;

    private byte id;
    
    public byte ID { get { return id; } }
    public TcpClient Tcp { get { return tcp; } }


    public ServerClient(TcpClient client, byte id)
    {
        this.tcp = client;
        this.id = id;
        this.ns = tcp.GetStream();
        this.stream = new BufferedStream(tcp.GetStream());
        
    }

    public Stream getStream() {
        return stream;
    }

    public bool IsDataAvailable()
    {
        return ns.DataAvailable;
    }

    public void Close()
    {
        tcp.Close();
    }

}
