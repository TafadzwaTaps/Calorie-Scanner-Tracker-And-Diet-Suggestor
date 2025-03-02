using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodLogsController : ControllerBase
    {
        private readonly CalorieTrackerContext _context;

        public FoodLogsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodLog>>> GetFoodLogs()
        {
            return await _context.FoodLogs.Include(f => f.Meal).Include(f => f.User).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<FoodLog>> PostFoodLog(FoodLog foodLog)
        {
            _context.FoodLogs.Add(foodLog);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetFoodLogs", new { id = foodLog.Id }, foodLog);
        }
    }
}
