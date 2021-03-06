﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using WebSocketSharp;

struct MessageHandler<T> {
    public string e;
    public T data;

    public MessageHandler (string _e, T _data) {
        e = _e;
        data = _data;
    }
}

public delegate void OnCallback (string data);
delegate void Lambda ();

public class ConnectionController : MonoBehaviour {
    WebSocket ws;
    public string url = "ws://localhost:3000";
    public bool connectOnStart = true;
    bool connected = false;
    Dictionary<string, List<OnCallback>> messegesCallback;

    Queue<Lambda> eventsQueue;

    void Awake () {
        messegesCallback = new Dictionary<string, List<OnCallback>> ();
        eventsQueue = new Queue<Lambda> ();

        ws = new WebSocket (url);

        ws.OnMessage += (sender, e) => {
            // Debug.Log ("uWebSocket Message: " + e.Data.ToString ());
            MessageHandler<string> reciveEvent = JsonConvert.DeserializeObject<MessageHandler<string>> (e.Data.ToString ());

            //Cabe refatoração
            // Para cada messageCallback
            messegesCallback.ToList ()
                // Procuramos os eventos correspondentes
                .FindAll ((value) => value.Key == reciveEvent.e)
                // Para cada evento
                .ForEach ((value) => {
                    // Para cada Callback de evento
                    value.Value.ForEach (cb => {
                        // Adicionamos uma função anonima
                        eventsQueue.Enqueue (() => {
                            // Que chama o callback com injeção da dependencia
                            cb (reciveEvent.data);
                        });
                    });
                });
        };
        ws.OnClose += (sender, e) => {
            Debug.LogWarning ("uWebSocket Close: " + e.Reason);
            connected = false;
        };
        ws.OnError += (sender, e) => {
            Debug.LogError ("uWebSocket Error: " + e.Exception + " message:" + e.Message);
        };
        ws.OnOpen += (sender, e) => {
            Debug.Log ("uWebSocket Connection Open");
        };
    }

    void Start () {
        if (connectOnStart)
            Connect ();

        StartCoroutine ("EventsLoop");
    }

    IEnumerator EventsLoop () {

        while (true) {
            if (eventsQueue.Count != 0) {
                eventsQueue.Dequeue () ();
            }
            yield return null;
        }
    }

    public void Connect () {
        if (!connected) {
            ws.Connect ();
            connected = true;
        }
    }

    public void Emit<T> (string e, T data) {
        MessageHandler<T> emit = new MessageHandler<T> (e, data);
        ws.Send (JsonConvert.SerializeObject (emit));
    }

    public void Emit (string e) {
        MessageHandler<string> emit = new MessageHandler<string> (e, "");
        ws.Send (JsonConvert.SerializeObject (emit));
    }

    public void On (string e, OnCallback cb) {
        if (messegesCallback.ContainsKey (e)) {
            messegesCallback[e].Add (cb);
            return;
        }
        messegesCallback.Add (e, new List<OnCallback> {
            cb
        });
    }

    public void OnConnect (EventHandler cb) {
        ws.OnOpen += cb;
    }

    public void OnDiconect (EventHandler<CloseEventArgs> cb) {
        ws.OnClose += cb;
    }
}