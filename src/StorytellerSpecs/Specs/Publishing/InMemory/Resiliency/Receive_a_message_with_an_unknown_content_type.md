# Receive a message with an unknown content type

-> id = b3a1e70a5-64d3-4af0-b905-0373593e8147
-> lifecycle = Regression
-> max-retries = 0
-> last-updated = 2017-04-18T15:00:48.0606694Z
-> tags =

[SendMessage]
|> IfTheApplicationIs
    [ServiceBusApplication]
    |> ListenForMessagesFrom
    ``` channel
    loopback://one
    ```

    |> SendMessage messageType=Message1
    ``` channel
    loopback://one
    ```

    |> SendMessage messageType=Message2
    ``` channel
    loopback://one
    ```


|> SendMessageWithUnknownContentType
``` address
loopback://one
```

|> SendMessage messageType=Message1, name=Suzy
|> SendMessage messageType=Message2, name=Russell
|> TheMessagesSentShouldBe
    [rows]
    |ReceivedAt                 |MessageType|Name   |
    |loopback://one|Message1   |Suzy   |
    |loopback://one|Message2   |Russell|

~~~
