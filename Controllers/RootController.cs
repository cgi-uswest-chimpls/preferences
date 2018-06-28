using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Preferences.Models;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Preferences.Controllers
{
    [Produces("application/json")]
    [Route("Root")]
    public class RootController : Controller
    {

		// TODO: Convert to Async await pattern, use constructor injection and create a repository layer

		public RootController(IOptions<CloudFoundryServicesOptions> serviceOptions)
		{
			var connectionString = serviceOptions
				.Value
				.Services
				.First(s => s.Label == "mlab")
				.Credentials["uri"]
				.Value;

			this.settings = new Settings() { 
				ConnectionString = connectionString, 
				Database = connectionString.Split("/").Last()
			};
		}

		private Settings settings { get; set; }

		//Generic method to get the mongodb database
		public IMongoDatabase GetMongoDatabase()
		{
			var mongoClient = new MongoClient(settings.ConnectionString);

			//  "mongodb://ServiceUser:e85tnbDnzZVK@ds163681.mlab.com:63681/CloudFoundry_6365383a_rj1due4v"

			return mongoClient.GetDatabase(settings.Database);
		}

        // GET ALL - /Root
    	[HttpGet]
		public JsonResult Get()
		{
			//Get the database connection
			var mongoDatabase = GetMongoDatabase();

			//fetch
			var result = mongoDatabase
                .GetCollection<UserPreferences>("Preferences")
                .Find(FilterDefinition<UserPreferences>.Empty)
                .ToList();

			return Json(result);
		}
		
        // GET ONE: Root/id
        [HttpGet("{id}", Name = "Get")]
        public JsonResult Get(int id)
        {
			//Get the database connection
			var mongoDatabase = GetMongoDatabase();

			//filter
			FilterDefinition<UserPreferences> filter = Builders<UserPreferences>.Filter.Eq(m => m.Id, id);

			//fetch
			var result = mongoDatabase
                .GetCollection<UserPreferences>("Preferences")
                .Find(filter)
                .FirstOrDefault();

			return Json(result);
        }
        
        // POST - CREATE: /Root
        [HttpPost]
        public void Post([FromBody] UserPreferences value)
        {
            //Get the database connection
			var mongoDatabase = GetMongoDatabase();
			mongoDatabase.GetCollection<UserPreferences>("Preferences").InsertOne(value);
        }
        
        // DELETE: Root/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            //Get the database connection
			var mongoDatabase = GetMongoDatabase();

			//Delete the customer record
			var result = mongoDatabase.GetCollection<UserPreferences>("Preferences").DeleteOne<UserPreferences>(k => k.Id == id);

			if (result.IsAcknowledged == false || result.DeletedCount == 0)
			{
				return BadRequest("Unable to Delete Customer id: " + id);
			}

            return Ok();
        }

        // PUT - UPDATE: Root/UserId
        [HttpPut()]
        public IActionResult Put([FromBody]UserPreferences value)
        {
			//Get the database connection
			var mongoDatabase = GetMongoDatabase();

			//Build the where condition
			var filter = Builders<UserPreferences>.Filter.Eq("Id", value.Id);

			//Build the update statement 
			var updatestatement = Builders<UserPreferences>.Update.Set("Id", value.Id);
			updatestatement = updatestatement.Set("Values", value.Values);

			//Update
			var result = mongoDatabase.GetCollection<UserPreferences>("Preferences").UpdateOne(filter, updatestatement);
			if (result.IsAcknowledged == false)
			{
				return BadRequest("Unable to update preferences, user id: " + value.Id);
			} 

			return Ok();
        }

		// TODO: Move this to models
		public class PreferenceValue
		{
			public string Key {get; set;}
			public string Value {get; set;}
		}

        [HttpPut("{id}")]
		public IActionResult Put(int id, [FromBody] PreferenceValue preferenceValue)
		{
			//Get the database connection
			var mongoDatabase = GetMongoDatabase();

			//filter
			FilterDefinition<UserPreferences> filter = Builders<UserPreferences>.Filter.Eq(m => m.Id, id);

			//fetch the existing preferences
			var currentPreferences = mongoDatabase
                .GetCollection<UserPreferences>("Preferences")
                .Find(filter)
                .FirstOrDefault();

			if(currentPreferences == null)
			{
				return BadRequest("Unable to update preferences, user id: " + id);
			}

			// Addd/Update the value
			currentPreferences.Values[preferenceValue.Key] = preferenceValue.Value;

			//Build the update statement 
			var updatestatement = Builders<UserPreferences>.Update.Set("Id", id);
			updatestatement = updatestatement.Set("Values", currentPreferences.Values);

			//Update
			var result = mongoDatabase.GetCollection<UserPreferences>("Preferences").UpdateOne(filter, updatestatement);

			if (result.IsAcknowledged == false)
			{
				return BadRequest("Unable to update preferences, user id: " + id);
			} 
			
			return Ok();
		}
    }
}
