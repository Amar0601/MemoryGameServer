using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MemoryGameServer.Controllers
{
    [EnableCors("MyPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class InitController : ControllerBase
    {
        private IConfiguration configuration;

        public InitController(IConfiguration config)
        {
            configuration = config;
        }

        [HttpPost]
        public ActionResult<String> Post([FromBody] Schema value)
        {
            try
            {
                // Generate new GUID for saving game data
                Guid gameId = Guid.NewGuid();
                // Read configuration value of data directory for saving the game data
                string directoryLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration["GameDataDirectory"]);
                // check if directory exist if not create it.
                if (!Directory.Exists(directoryLocation))
                {
                    Directory.CreateDirectory(directoryLocation);
                }

                string fileName = gameId + ".json";
                string filePath = Path.Combine(directoryLocation, fileName);
                // create file at filePath formed in earliar step
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    value.roundData = new List<KeyValuePair<string, string>>();
                    stream.Write(Encoding.ASCII.GetBytes(value.ToString()));
                }

                // return GUID back to the client
                return JsonConvert.SerializeObject(new { gameId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while creating datafile: " + ex.Message.ToString());
                throw;
            }
        }

        [HttpPut]
        public ActionResult<String> Put([FromBody] Round value)
        {

            // set value based on equality
            value.match = value.card1.Equals(value.card2) ? true : false;
            // filename to read data from
            string fileName = value.game + ".json";
            // file location
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuration["GameDataDirectory"], fileName);

            try
            {
                // read all contents from game datafile
                var json = System.IO.File.ReadAllText(filePath);
                var jsonObj = JObject.Parse(json);
                // takeout rounds data for game
                var rounds = jsonObj.GetValue("roundData") as JArray;
                // add new round data into existing object
                var newRoundData = JObject.Parse(JsonConvert.SerializeObject(new KeyValuePair<string, string>(value.card1, value.card2)));
                rounds.Add(newRoundData);

                jsonObj["roundData"] = rounds;
                string updatedData = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj,
                                       Newtonsoft.Json.Formatting.Indented);
                // write updated data into file back
                System.IO.File.WriteAllText(filePath, updatedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured while adding round data: " + ex.Message.ToString());
            }

            // return equality of file
            return JsonConvert.SerializeObject(value);
        }
    }


    // these classes will help us in databinding
    public class Schema
    {
        public string level { get; set; }
        public List<KeyValuePair<string, string>> roundData { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Round
    {
        public Guid game;
        public string card1;
        public string card2;
        public bool? match;
    }
}