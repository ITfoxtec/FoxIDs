using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Controllers
{
    public class TValuesController : ControllerBase
    {
        // GET api/values
        // GET api/values/5
        public ActionResult<IActionResult> GetValues(int? id)
        {
            if(!id.HasValue)
            {
                return Ok(new string[] { "value1", "value2" });
            }
            else
            {
                return Ok("value");
            }
        }

        // POST api/values
        public void PostValues([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void PutValues(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void DeleteValues(int id)
        {
        }
    }
}
