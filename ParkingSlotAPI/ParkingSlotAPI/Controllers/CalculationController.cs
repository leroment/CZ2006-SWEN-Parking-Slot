﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkingSlotAPI.Helpers;
using ParkingSlotAPI.Models;
using ParkingSlotAPI.Repository;
using ParkingSlotAPI.Services;
using ParkingSlotAPI.Entities;
using System.Collections;
using System.Globalization;

namespace ParkingSlotAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CalculationController : ControllerBase
	{
		private readonly ICarparkRatesRepository ICarparkRateRepository;
		private readonly IMapper _mapper;

		public CalculationController(ICarparkRatesRepository calRepository, IMapper mapper)
		{
			ICarparkRateRepository = calRepository;
			_mapper = mapper;
		}

        [HttpGet("calculate/{id}")]
        public IActionResult CalculateCarparkPrice(Guid id, [FromQuery] DateTime StartTime, [FromQuery] DateTime EndTime, [FromQuery] String vehicleType)
        {
            double price = 0.0;
            // get the carpark rates from the specified carpark
            var carparkRatesRecords = ICarparkRateRepository.GetCarparkRateById(id, vehicleType).ToList();

            // start time component
            var startTimeDayComponent = StartTime.DayOfWeek;
            var startTimeHourComponent = StartTime.Hour;
            var startTimeMinuteComponent = StartTime.Minute;


            // end time component
            var endTimeDayComponent = EndTime.DayOfWeek;
            var endTimeHourComponent = EndTime.Hour;
            var endTimeMinuteComponent = EndTime.Minute;

            // get duration
            var duration = EndTime - StartTime;


            // if the start time and end time are in the same day
            if (StartTime.DayOfWeek == EndTime.DayOfWeek)
            {
                var flatRateCarparkRatesRecords = carparkRatesRecords.Where(a => Double.Parse(a.WeekdayMin.Substring(0, a.WeekdayMin.Length - 5)) > 30).ToList();

                var startTimeWithinCarparkRatesRecords = carparkRatesRecords.Where(a => StartTime.TimeOfDay >= DateTime.Parse(a.StartTime).TimeOfDay).ToList();

                var endTimeWithinCarparkRatesRecords = carparkRatesRecords.Where(a => EndTime.TimeOfDay <= DateTime.Parse(a.EndTime).TimeOfDay).ToList();

                var startTimeOutsideCarparkRatesRecords = carparkRatesRecords.Where(a => StartTime.TimeOfDay < DateTime.Parse(a.EndTime).TimeOfDay).Except(endTimeWithinCarparkRatesRecords).Except(flatRateCarparkRatesRecords).ToList();

                var endTimeOutsideCarparkRatesRecords = carparkRatesRecords.Where(a => EndTime.TimeOfDay > DateTime.Parse(a.StartTime).TimeOfDay).ToList().Except(startTimeWithinCarparkRatesRecords).Except(flatRateCarparkRatesRecords).ToList();

                if (endTimeOutsideCarparkRatesRecords.Count() == endTimeWithinCarparkRatesRecords.Count())
                {
                    endTimeOutsideCarparkRatesRecords.Clear();
                }

                if (startTimeOutsideCarparkRatesRecords.Count() == startTimeWithinCarparkRatesRecords.Count())
                {
                    startTimeOutsideCarparkRatesRecords.Clear();
                }
                
                if (startTimeOutsideCarparkRatesRecords.Count() == endTimeOutsideCarparkRatesRecords.Count())
                {
                    startTimeOutsideCarparkRatesRecords.RemoveAt(startTimeOutsideCarparkRatesRecords.Count - 1);
                    endTimeOutsideCarparkRatesRecords.RemoveAt(0);
                }

                // if only one record is shown, this means time is within the database timeframe.
                if (startTimeWithinCarparkRatesRecords.Count() == 1 && endTimeWithinCarparkRatesRecords.Count() == 1 && startTimeOutsideCarparkRatesRecords.Count() == 0 && endTimeOutsideCarparkRatesRecords.Count() == 0)
                {
                    if (startTimeDayComponent == DayOfWeek.Saturday)
                    {
                        price = duration.TotalMinutes / Double.Parse(startTimeWithinCarparkRatesRecords[0].SatdayMin.Substring(0, startTimeWithinCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                    }
                    else if (startTimeDayComponent == DayOfWeek.Sunday)
                    {
                        price = duration.TotalMinutes / Double.Parse(startTimeWithinCarparkRatesRecords[0].SunPHMin.Substring(0, startTimeWithinCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                    }
                    else
                    {
                        price = duration.TotalMinutes / Double.Parse(startTimeWithinCarparkRatesRecords[0].WeekdayMin.Substring(0, startTimeWithinCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                    }
                }

                if (startTimeOutsideCarparkRatesRecords.Count() == 1 && endTimeWithinCarparkRatesRecords.Count() == 1 && startTimeWithinCarparkRatesRecords.Count() == 0 && endTimeOutsideCarparkRatesRecords.Count() == 0)
                {
                    var fromStartTimeToDatabaseEndTimeDuration = (DateTime.Parse(startTimeOutsideCarparkRatesRecords[0].EndTime).TimeOfDay - StartTime.TimeOfDay).TotalMinutes;

                    var fromDatabaseStartTimetoEndTimeDuration = (EndTime.TimeOfDay - DateTime.Parse(endTimeWithinCarparkRatesRecords[0].StartTime).TimeOfDay).TotalMinutes;

                    if (startTimeDayComponent == DayOfWeek.Saturday)
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimetoEndTimeDuration / Double.Parse(endTimeWithinCarparkRatesRecords[0].SatdayMin.Substring(0, endTimeWithinCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(endTimeWithinCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                    }
                    else if (startTimeDayComponent == DayOfWeek.Sunday)
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                        price += fromDatabaseStartTimetoEndTimeDuration / Double.Parse(endTimeWithinCarparkRatesRecords[0].SunPHMin.Substring(0, endTimeWithinCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(endTimeWithinCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                    }
                    else
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimetoEndTimeDuration / Double.Parse(endTimeWithinCarparkRatesRecords[0].WeekdayMin.Substring(0, endTimeWithinCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(endTimeWithinCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                    }
                }

                if (startTimeWithinCarparkRatesRecords.Count() == 1 && endTimeOutsideCarparkRatesRecords.Count() == 1 && startTimeOutsideCarparkRatesRecords.Count() == 0 && endTimeWithinCarparkRatesRecords.Count() == 0)
                {
                    var fromStartTimeToDatabaseEndTimeDuration = (DateTime.Parse(startTimeWithinCarparkRatesRecords[0].EndTime).TimeOfDay - StartTime.TimeOfDay).TotalMinutes;

                    var fromDatabaseStartTimeToEndTimeDuration = (EndTime.TimeOfDay - DateTime.Parse(endTimeOutsideCarparkRatesRecords[0].StartTime).TimeOfDay).TotalMinutes;

                    if (startTimeDayComponent == DayOfWeek.Saturday)
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeWithinCarparkRatesRecords[0].SatdayMin.Substring(0, startTimeWithinCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTimeDuration / Double.Parse(endTimeOutsideCarparkRatesRecords[0].SatdayMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].SatdayRate.Remove(0, 1));

                    }
                    else if (startTimeDayComponent == DayOfWeek.Sunday)
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeWithinCarparkRatesRecords[0].SunPHMin.Substring(0, startTimeWithinCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTimeDuration / Double.Parse(endTimeOutsideCarparkRatesRecords[0].SunPHMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].SunPHRate.Remove(0, 1));

                    }
                    else
                    {
                        price += fromStartTimeToDatabaseEndTimeDuration / Double.Parse(startTimeWithinCarparkRatesRecords[0].WeekdayMin.Substring(0, startTimeWithinCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(startTimeWithinCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTimeDuration / Double.Parse(endTimeOutsideCarparkRatesRecords[0].WeekdayMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));

                    }
                }

                if (startTimeOutsideCarparkRatesRecords.Count() == 1 && endTimeOutsideCarparkRatesRecords.Count() == 1 && startTimeWithinCarparkRatesRecords.Count() == 0 && endTimeWithinCarparkRatesRecords.Count() == 0)
                {
                    var fromStartTimeToDatabaseEndTime = (DateTime.Parse(startTimeOutsideCarparkRatesRecords[0].EndTime).TimeOfDay - StartTime.TimeOfDay).TotalMinutes;
                    var fromDatabaseStartTimeToDatabaseEndTime = (DateTime.Parse(endTimeOutsideCarparkRatesRecords[0].EndTime) - DateTime.Parse(endTimeOutsideCarparkRatesRecords[0].StartTime)).TotalMinutes;
                    var fromDatabaseStartTimeToEndTime = (EndTime.TimeOfDay - DateTime.Parse(startTimeOutsideCarparkRatesRecords[0].StartTime).TimeOfDay).TotalMinutes;

                    if (startTimeDayComponent == DayOfWeek.Saturday)
                    {
                        price += fromStartTimeToDatabaseEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToDatabaseEndTime / Double.Parse(endTimeOutsideCarparkRatesRecords[0].SatdayMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SatdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SatdayRate.Remove(0, 1));
                    }
                    else if (startTimeDayComponent == DayOfWeek.Sunday)
                    {
                        price += fromStartTimeToDatabaseEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToDatabaseEndTime / Double.Parse(endTimeOutsideCarparkRatesRecords[0].SunPHMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].SunPHMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].SunPHRate.Remove(0, 1));
                    }
                    else
                    {
                        price += fromStartTimeToDatabaseEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToDatabaseEndTime / Double.Parse(endTimeOutsideCarparkRatesRecords[0].WeekdayMin.Substring(0, endTimeOutsideCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(endTimeOutsideCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                        price += fromDatabaseStartTimeToEndTime / Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Substring(0, startTimeOutsideCarparkRatesRecords[0].WeekdayMin.Length - 5)) * Double.Parse(startTimeOutsideCarparkRatesRecords[0].WeekdayRate.Remove(0, 1));
                    }
                }
            }
            else
            {
                
            }


            return Ok(new { Price = price });
        }

        [HttpGet("{id}")]
        public ActionResult Index(Guid id, [FromQuery] DateTime StartTime, [FromQuery] DateTime EndTime, [FromQuery] String vehicleType)
        {
            var duration = 0.0;

            double Price = 0;
            Boolean IsNUll = false, NonExistenceCarparkRate = false, InvalidDate = false, FlatRateExistence = false;
            DateTime RateStartTimeFromDB = new DateTime(1, 1, 1); ;
            DateTime RateEndTimeFromDB = new DateTime(1, 1, 1);
            var carpark = ICarparkRateRepository.GetCarparkRateById(id, vehicleType);
            List<CarparkRate> CarParkRateList = carpark.ToList();

            if (EndTime.ToString() == "1/1/0001 12:00:00 AM" && StartTime.ToString() == "1/1/0001 12:00:00 AM")
            {
                duration = 60;
            }
            else if (EndTime.ToString() != "1/1/0001 12:00:00 AM" && StartTime.ToString() == "1/1/0001 12:00:00 AM")
            {
                StartTime = DateTime.Now;
                StartTime = new DateTime(StartTime.Year, StartTime.Month, StartTime.Day, StartTime.Hour, StartTime.Minute, 0);

                duration = (EndTime - StartTime).TotalMinutes;
            }
            else
            {
                duration = (EndTime - StartTime).TotalMinutes;
            }
            if (duration >= 0)
            {
                if (CarParkRateList.Count != 0)
                {

                    var result = new Calculation(StartTime, (int)duration);
                    List<HoursPerDay> dayOfWeek = result.getparkingDay(StartTime, EndTime);


                    foreach (HoursPerDay EachHoursPerDay in dayOfWeek)
                    {
                        if (EachHoursPerDay.getDay() > 0 && EachHoursPerDay.getDay() < 6)
                        {
                            //weekdays calculation
                            double TimeChecker = EachHoursPerDay.getDayDuration();

                            while (TimeChecker != 0)
                            {
                                int checkAllIfFailed = 0;
                                for (int i = 0; i < CarParkRateList.Count; i++)
                                {
                                    if ((Convert.ToInt32(CarParkRateList[i].WeekdayMin.Trim('m', 'i', 'n', 's'))) <= 30)
                                    {
                                        RateStartTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getStartTimeOfTheDay().Day + "/" + EachHoursPerDay.getStartTimeOfTheDay().Month + "/" + EachHoursPerDay.getStartTimeOfTheDay().Year + " " + CarParkRateList[i].StartTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        DateTime u = DateTime.ParseExact(CarParkRateList[i].EndTime, "HH:mm:ss", CultureInfo.InvariantCulture);
                                        if (u.TimeOfDay.TotalMinutes < 1440)
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }
                                        else
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day - 1 + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }

                                        double durationOfStaticTimeInMin = result.getPeriodDuration(RateStartTimeFromDB, RateEndTimeFromDB);
                                        double durationOfDynamicTimeInMin = result.getPeriodDuration(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay());


                                        if (RateStartTimeFromDB.TimeOfDay == EachHoursPerDay.getStartTimeOfTheDay().TimeOfDay &&
                                            durationOfDynamicTimeInMin <= durationOfStaticTimeInMin && TimeChecker > 0)
                                        {
                                            result.setDuration((int)EachHoursPerDay.getDayDuration());
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].WeekdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].WeekdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= durationOfDynamicTimeInMin;
                                            checkAllIfFailed--;
                                        }

                                        else if (TimeChecker > 0 && result.TimePeriodOverlaps(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {


                                            result.setDuration((int)(EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].WeekdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].WeekdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= (int)(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - (EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);

                                            checkAllIfFailed--;

                                        }
                                        else if (TimeChecker > 0 && result.TimePeriodOverlapsRight(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {
                                            double redundantValue = (double)(EachHoursPerDay.getEndTimeOfTheDay() - RateEndTimeFromDB).TotalMinutes;
                                            result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));

                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].WeekdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].WeekdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));

                                            checkAllIfFailed--;

                                        }
                                        /*else if (TimeChecker > 0 && result.TimePeriodOverlapsLeft(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == true)
										{
											double redundantValue = (double)(RateStartTimeFromDB - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes;
											result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
											TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));
										}*/

                                        if (result.TimePeriodOverlaps(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == false)
                                        {
                                            checkAllIfFailed++;


                                        }
                                        if (TimeChecker == 0)
                                        {
                                            break;
                                        }
                                    }
                                    else if ((Convert.ToInt32(CarParkRateList[i].WeekdayMin.Trim('m', 'i', 'n', 's'))) > 30)
                                    {
                                        FlatRateExistence = true;
                                    }

                                }
                                if (checkAllIfFailed >= CarParkRateList.Count)
                                {
                                    NonExistenceCarparkRate = true;

                                    break;
                                }
                                /*	else if(FlatRateExistence==true&& TimeChecker!=0)
                                    {
                                    }*/
                            }


                        }
                        else if (EachHoursPerDay.getDay() == 6)
                        {
                            //Sat calculation
                            double TimeChecker = EachHoursPerDay.getDayDuration();

                            while (TimeChecker != 0)
                            {
                                int checkAllIfFailed = 0;
                                for (int i = 0; i < CarParkRateList.Count; i++)
                                {
                                    if ((Convert.ToInt32(CarParkRateList[i].SatdayMin.Trim('m', 'i', 'n', 's'))) <= 30)
                                    {
                                        RateStartTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getStartTimeOfTheDay().Day + "/" + EachHoursPerDay.getStartTimeOfTheDay().Month + "/" + EachHoursPerDay.getStartTimeOfTheDay().Year + " " + CarParkRateList[i].StartTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        DateTime u = DateTime.ParseExact(CarParkRateList[i].EndTime, "HH:mm:ss", CultureInfo.InvariantCulture);
                                        if (u.TimeOfDay.TotalMinutes < 1440)
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }
                                        else
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day - 1 + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }

                                        double durationOfStaticTimeInMin = result.getPeriodDuration(RateStartTimeFromDB, RateEndTimeFromDB);
                                        double durationOfDynamicTimeInMin = result.getPeriodDuration(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay());


                                        if (RateStartTimeFromDB.TimeOfDay == EachHoursPerDay.getStartTimeOfTheDay().TimeOfDay &&
                                            durationOfDynamicTimeInMin <= durationOfStaticTimeInMin && TimeChecker > 0)
                                        {
                                            result.setDuration((int)EachHoursPerDay.getDayDuration());
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SatdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SatdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= durationOfDynamicTimeInMin;
                                            checkAllIfFailed--;
                                        }

                                        else if (TimeChecker > 0 && result.TimePeriodOverlaps(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {


                                            result.setDuration((int)(EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SatdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SatdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= (int)(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - (EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);

                                            checkAllIfFailed--;

                                        }
                                        else if (TimeChecker > 0 && result.TimePeriodOverlapsRight(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {
                                            double redundantValue = (double)(EachHoursPerDay.getEndTimeOfTheDay() - RateEndTimeFromDB).TotalMinutes;
                                            result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));

                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SatdayRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SatdayMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));

                                            checkAllIfFailed--;

                                        }
                                        /*else if (TimeChecker > 0 && result.TimePeriodOverlapsLeft(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == true)
										{
											double redundantValue = (double)(RateStartTimeFromDB - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes;
											result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
											TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));
										}*/

                                        if (result.TimePeriodOverlaps(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == false)
                                        {
                                            checkAllIfFailed++;


                                        }
                                        if (TimeChecker == 0)
                                        {
                                            break;
                                        }
                                    }
                                    else if ((Convert.ToInt32(CarParkRateList[i].SatdayMin.Trim('m', 'i', 'n', 's'))) > 30)
                                    {
                                        FlatRateExistence = true;
                                    }


                                }
                                if (checkAllIfFailed >= CarParkRateList.Count)
                                {
                                    NonExistenceCarparkRate = true;

                                    break;
                                }
                                /*	else if(FlatRateExistence==true&& TimeChecker!=0)
								{
								}*/
                            }
                        }
                        else
                        {
                            //SUN and PH calculation

                            double TimeChecker = EachHoursPerDay.getDayDuration();

                            while (TimeChecker != 0)
                            {
                                int checkAllIfFailed = 0;
                                for (int i = 0; i < CarParkRateList.Count; i++)
                                {

                                    if ((Convert.ToInt32(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's'))) <= 30)
                                    {
                                        RateStartTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getStartTimeOfTheDay().Day + "/" + EachHoursPerDay.getStartTimeOfTheDay().Month + "/" + EachHoursPerDay.getStartTimeOfTheDay().Year + " " + CarParkRateList[i].StartTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        DateTime u = DateTime.ParseExact(CarParkRateList[i].EndTime, "HH:mm:ss", CultureInfo.InvariantCulture);
                                        if (u.TimeOfDay.TotalMinutes < 1440)
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }
                                        else
                                        {
                                            RateEndTimeFromDB = DateTime.ParseExact(EachHoursPerDay.getEndTimeOfTheDay().Day - 1 + "/" + EachHoursPerDay.getEndTimeOfTheDay().Month + "/" + EachHoursPerDay.getEndTimeOfTheDay().Year + " " + CarParkRateList[i].EndTime, "d/M/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                                        }

                                        double durationOfStaticTimeInMin = result.getPeriodDuration(RateStartTimeFromDB, RateEndTimeFromDB);
                                        double durationOfDynamicTimeInMin = result.getPeriodDuration(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay());


                                        if (RateStartTimeFromDB.TimeOfDay == EachHoursPerDay.getStartTimeOfTheDay().TimeOfDay &&
                                            durationOfDynamicTimeInMin <= durationOfStaticTimeInMin && TimeChecker > 0)
                                        {
                                            result.setDuration((int)EachHoursPerDay.getDayDuration());
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= durationOfDynamicTimeInMin;
                                            checkAllIfFailed--;
                                        }

                                        else if (TimeChecker > 0 && result.TimePeriodOverlaps(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {


                                            result.setDuration((int)(EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);
                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= (int)(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - (EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes);

                                            checkAllIfFailed--;

                                        }
                                        else if (TimeChecker > 0 && result.TimePeriodOverlapsRight(EachHoursPerDay.getStartTimeOfTheDay(), EachHoursPerDay.getEndTimeOfTheDay(), RateStartTimeFromDB, RateEndTimeFromDB) == true)
                                        {
                                            double redundantValue = (double)(EachHoursPerDay.getEndTimeOfTheDay() - RateEndTimeFromDB).TotalMinutes;
                                            result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));

                                            Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
                                            TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
                                            EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));

                                            checkAllIfFailed--;

                                        }
                                        /*else if (TimeChecker > 0 && result.TimePeriodOverlapsLeft(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == true)
										{
											double redundantValue = (double)(RateStartTimeFromDB - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes;
											result.setDuration((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											Price += result.calculatePrice(Convert.ToDouble(CarParkRateList[i].SunPHRate.Trim('$')), Convert.ToDouble(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's')));
											TimeChecker -= ((int)((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setDayDuration(EachHoursPerDay.getDayDuration() - ((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue));
											EachHoursPerDay.setStartTimeOfTheDay(EachHoursPerDay.getStartTimeOfTheDay().AddMinutes(((EachHoursPerDay.getEndTimeOfTheDay() - EachHoursPerDay.getStartTimeOfTheDay()).TotalMinutes - redundantValue)));
										}*/

                                        if (result.TimePeriodOverlaps(StartTime, EndTime, RateStartTimeFromDB, RateEndTimeFromDB) == false)
                                        {
                                            checkAllIfFailed++;


                                        }
                                        if (TimeChecker == 0)
                                        {
                                            break;
                                        }
                                    }
                                    else if ((Convert.ToInt32(CarParkRateList[i].SunPHMin.Trim('m', 'i', 'n', 's'))) > 30)
                                    {
                                        FlatRateExistence = true;
                                    }

                                }
                                if (checkAllIfFailed >= CarParkRateList.Count)
                                {
                                    NonExistenceCarparkRate = true;

                                    break;
                                }
                                /*	else if(FlatRateExistence==true&& TimeChecker!=0)
								{
								}*/
                            }
                        }

                    }


                }
                else
                {
                    IsNUll = true;
                    Price = 0;
                }
            }
            else if (0 < duration)
            {
                InvalidDate = true;
            }

            return Ok(new { id, duration, Price, StartTime, EndTime, vehicleType, IsNUll, NonExistenceCarparkRate, InvalidDate });

        }


    }

	}