using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HubShared;

internal class Db
{
    static MongoClient _Client = new MongoClient(@"mongodb://iansdesktop");
    static IMongoDatabase _Database = _Client.GetDatabase("HubServer");
    static IMongoCollection<Message> _messages = _Database.GetCollection<Message>("Messages");

    public static void Log(HubMessage message, MessageDirection direction)
    {
        var log = new Message
        {
            Direction = direction,
            Type = message.GetType().FullName,
            Args = message.Args,
        };

        foreach (var prop in message.GetType().GetProperties())
        {
            var value = prop.GetValue(message, null);
            log.CatchAll.Add(prop.Name, value);
        }

        _messages.InsertOneAsync(log);
    }
}

class Message
{
    public ObjectId Id { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public MessageDirection Direction { get; set; }
    public string Type { get; set; }

    public string[] Args { get; set; }

    [BsonExtraElements]
    public Dictionary<string, object?> CatchAll { get; set; } = new Dictionary<string, object?>();
}
public enum MessageDirection { In, Out }

