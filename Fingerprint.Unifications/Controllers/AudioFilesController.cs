using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fingerprint.Unifications;
using Fingerprint.Unifications.Models;

namespace Fingerprint.Unifications.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioFilesController : ControllerBase
    {
        private readonly FingerprintDatabaseContext _context;

        public AudioFilesController(FingerprintDatabaseContext context)
        {
            _context = context;
        }

        // GET: api/AudioFiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AudioFile>>> GetAudioFiles()
        {
            return await _context.AudioFiles.ToListAsync();
        }

        // GET: api/AudioFiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AudioFile>> GetAudioFile(int id)
        {
            var audioFile = await _context.AudioFiles.FindAsync(id);

            if (audioFile == null)
            {
                return NotFound();
            }

            return audioFile;
        }

        // PUT: api/AudioFiles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAudioFile(int id, AudioFile audioFile)
        {
            if (id != audioFile.IdAudio)
            {
                return BadRequest();
            }

            _context.Entry(audioFile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AudioFileExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/AudioFiles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AudioFile>> PostAudioFile(AudioFile audioFile)
        {
            _context.AudioFiles.Add(audioFile);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (AudioFileExists(audioFile.IdAudio))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetAudioFile", new { id = audioFile.IdAudio }, audioFile);
        }

        // DELETE: api/AudioFiles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAudioFile(int id)
        {
            var audioFile = await _context.AudioFiles.FindAsync(id);
            if (audioFile == null)
            {
                return NotFound();
            }

            _context.AudioFiles.Remove(audioFile);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AudioFileExists(int id)
        {
            return _context.AudioFiles.Any(e => e.IdAudio == id);
        }
    }
}
