using MeetingScheduler.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using MeetingScheduler.Data;
using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore; // Zamień na swoją przestrzeń nazw

public class MeetingController : Controller
{
    private readonly AppDbContext _context;

    public MeetingController(AppDbContext context)
    {
        _context = context;
    }

    // Wyświetlanie wszystkich spotkań
    public ActionResult Index()
    {
        var meetings = _context.Meetings.ToList();
        return View(meetings);
    }

    // Formularz dodawania nowego spotkania
    public ActionResult Create()
    {
        return View();
    }

    // Obsługa przesyłania formularza
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Meeting meeting)
    {
        if (ModelState.IsValid)
        {
            _context.Meetings.Add(meeting);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        return View(meeting);
    }

    // Akcja do wyświetlania formularza edycji spotkania
    public IActionResult Edit(int id)
    {
        var meeting = _context.Meetings.FirstOrDefault(m => m.Id == id);

        if (meeting == null)
        {
            return NotFound(); // Jeśli spotkanie o podanym ID nie istnieje
        }

        return View(meeting);
    }

    // Akcja do zapisywania edytowanego spotkania
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, Meeting meeting)
    {
        if (id != meeting.Id)
        {
            return NotFound(); // Sprawdzenie, czy ID spotkania jest zgodne
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(meeting);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Meetings.Any(m => m.Id == meeting.Id))
                {
                    return NotFound(); // Jeśli spotkanie nie istnieje
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index)); // Po zapisaniu przekierowanie do listy spotkań
        }

        return View(meeting); // Jeśli wystąpiły błędy walidacji, ponownie wyświetlamy formularz edycji
    }

    // Akcja do usuwania spotkania
    public IActionResult Delete(int id)
    {
        var meeting = _context.Meetings.FirstOrDefault(m => m.Id == id);

        if (meeting == null)
        {
            return NotFound(); // Jeśli spotkanie o podanym ID nie istnieje
        }

        return View(meeting);
    }

    // Akcja do potwierdzenia usunięcia spotkania
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var meeting = _context.Meetings.FirstOrDefault(m => m.Id == id);
        if (meeting != null)
        {
            _context.Meetings.Remove(meeting);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index)); // Po usunięciu przekierowanie do listy spotkań
    }


    // API do zwracania spotkań dla FullCalendar
    public JsonResult GetEvents()
    {
        var events = _context.Meetings.Select(m => new
        {
            title = m.Title,
            start = m.Start,
            end = m.End,
            description = m.Description
        }).ToList();

        return Json(events);
    }

    //WGRYWANIE PLIKÓW CSV I JSON

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Wybierz plik do importu.");
            return View();
        }

        try
        {
            // Debugowanie, sprawdzanie nazwy pliku
            Console.WriteLine($"Importing file: {file.FileName}");

            if (file.FileName.EndsWith(".csv"))
            {
                var meetings = ImportFromCsv(file);
                _context.Meetings.AddRange(meetings);
            }
            else if (file.FileName.EndsWith(".json"))
            {
                var meetings = ImportFromJson(file);
                _context.Meetings.AddRange(meetings);
            }
            else
            {
                ModelState.AddModelError("", "Obsługiwane są tylko pliki CSV i JSON.");
                return View();
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            // Złapanie wyjątku
            Console.WriteLine($"Błąd importu: {ex.Message}");
            ModelState.AddModelError("", $"Wystąpił błąd podczas importu: {ex.Message}");
            return View();
        }
    }


    private List<Meeting> ImportFromCsv(IFormFile file)
    {
        var meetings = new List<Meeting>();

        using (var reader = new StreamReader(file.OpenReadStream()))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<Meeting>().ToList();
            meetings.AddRange(records);
        }

        return meetings;
    }

    private List<Meeting> ImportFromJson(IFormFile file)
    {
        var meetings = new List<Meeting>();

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            var json = reader.ReadToEnd();
            meetings = JsonConvert.DeserializeObject<List<Meeting>>(json);
        }

        return meetings;
    }


}
