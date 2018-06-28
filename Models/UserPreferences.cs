using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Preferences.Models
{
    public class UserPreferences
	{
		[BsonId]
		public int Id { get; set; }

		[BsonElement]
		public Dictionary<string, string> Values { get; set; }
	}
}