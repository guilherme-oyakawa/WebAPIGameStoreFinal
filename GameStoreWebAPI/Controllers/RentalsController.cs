﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using GameStoreWebAPI.DAL;
using GameStoreWebAPI.DAL.Repositories;
using GameStoreWebAPI.Models;
using GameStoreWebAPI.Models.DTO.Rental;

namespace GameStoreWebAPI.Controllers
{
    public class RentalsController : ApiController
    {
        private IRentalRepository rep;
        //private StoreContext db = new StoreContext();

        public RentalsController(){
            this.rep = new RentalRepository(new StoreContext());
        }

        // GET: api/Rentals
        [HttpGet]
        [ActionName("getRentals")]
        public IEnumerable<RentalDTO> GetRentals()
        {
            var query = from r in rep.GetRentals()
                        select r.toDTO();
            return query;
        }

        // GET: api/Rentals
        [HttpGet]
        [ActionName("getCurrentRentals")]
        public IEnumerable<RentalDTO> CurrentRentals() {
            var query = from r in rep.GetCurrentRentals()
                        select r.toDTO();
            return query;
        }

        // GET: api/Rentals
        [HttpGet]
        [ActionName("getPastRentals")]
        public IEnumerable<RentalDTO> PastRentals() {
            var query = from r in rep.GetReturnedRentals()
                        select r.toDTO();
            return query;
        }

        // GET: api/Rentals/5
        [HttpGet]
        [ActionName("getRental")]
        [ResponseType(typeof(RentalDTO))]
        public async Task<IHttpActionResult> GetRental(int id)
        {
            Rental rental = await rep.GetRentalAsync(id);
            if (rental == null)
            {
                return NotFound();
            }
            return Ok(rental.toDTO());
        }

        // PUT: api/Rentals/5
        [HttpPut]
        [ActionName("updateRental")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutRental(int id, Rental rental)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != rental.RentalID)
            {
                return BadRequest();
            }

            rep.UpdateRental(rental);
            //db.Entry(rental).State = EntityState.Modified;

            try
            {
                await rep.SaveAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RentalExists(id))
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

        // PUT: api/Rentals/5
        [HttpPut]
        [ActionName("returnRental")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> ReturnRental(int id) {
            Rental rental = await rep.GetRentalAsync(id);
            if (rental == null) {
                return NotFound();
            }
            DateTime now = DateTime.Now;
            rental.ReturnedOn = now;
            rep.ReturnCopy(rental.CopyID);
            if (rental.RentalFee > 0) {
                Fee fee = new Fee {
                    RentalID = rental.RentalID,
                    Rental = rental,
                    Value = rental.RentalFee,
                    Paid = false
                };
                rep.InsertFee(fee);
                await rep.SaveAsync();
            };
            
            rep.UpdateRental(rental);
            await rep.SaveAsync();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Rentals
        [HttpPost]
        [ActionName("insertRental")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PostRental(Rental rental)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //Take the copy
            Copy copy = await rep.GetCopyAsync(rental.CopyID);
            copy.Available = false;
            rep.UpdateCopy(copy);
            await rep.SaveAsync();

            //creates rental
            rep.InsertRental(rental);
            await rep.SaveAsync();

            return Ok();
        }

        // DELETE: api/Rentals/5
        [HttpDelete]
        [ActionName("deleteRental")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> DeleteRental(int id)
        {
            Rental rental = await rep.GetRentalAsync(id);
            if (rental == null)
            {
                return NotFound();
            }

            rep.DeleteRental(id);
            await rep.SaveAsync();

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rep.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool RentalExists(int id)
        {
            return rep.GetRentals().Count(e => e.RentalID == id) > 0;
        }
    }
}