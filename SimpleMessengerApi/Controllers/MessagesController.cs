using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using SimpleMessengerApi.Models;

namespace SimpleMessengerApi.Controllers
{
    public class MessagesController : ApiController
    {
        private SimpleMessengerDbEntities db = new SimpleMessengerDbEntities();

        // GET: api/Messages
        public IQueryable<Message> GetMessage()
        {
            return db.Message;
        }

        // GET: api/Messages/5
        [ResponseType(typeof(IEnumerable<Message>))]
        public IHttpActionResult GetMessage(int id, int uid)
        {
            List<User> users = db.Device.FirstOrDefault(d => d.Id == id).User.ToList();
            User user;
            try
            {
                user = users[uid];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Ok(new List<Message>());
            }
            IEnumerable<Message> message = db.Message.Where(m => m.DeviceId == id && m.User.Id == user.Id);
            if (message == null)
            {
                return NotFound();
            }

            return Ok(message);
        }

        // PUT: api/Messages/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutMessage(int id, Message message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != message.Id)
            {
                return BadRequest();
            }

            db.Entry(message).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MessageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Messages
        [ResponseType(typeof(Message))]
        public IHttpActionResult PostMessage(int did, int uid, string message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            List<User> users = db.Device.FirstOrDefault(d => d.Id == did).User.ToList();
            User user;
            try
            {
                user = users[uid];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return NotFound();
            }
            Messenger.Send(user.Id, $"Пользователь устройства №{did} прислал вам сообщение:\n\n" + message);

            //db.Message.Add(message);
            //db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = message }, message);
        }

        // DELETE: api/Messages/5
        [ResponseType(typeof(Message))]
        public IHttpActionResult DeleteMessage(int id)
        {
            Message message = db.Message.Find(id);
            if (message == null)
            {
                return NotFound();
            }

            db.Message.Remove(message);
            db.SaveChanges();

            return Ok(message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool MessageExists(int id)
        {
            return db.Message.Count(e => e.Id == id) > 0;
        }
    }
}