﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Tesseract;
using WeDoDigi_intern.CRUD_Service;
using WeDoDigi_intern.Models;

namespace WeDoDigi_intern.Controllers
{
    public class RecipeController : Controller
    {
        // private RecipeHolder recipes
        private readonly RecipeDbService rDbService;
        private readonly TestCrud tCrud;
        private readonly TagCrud tagService;

        public RecipeController(RecipeDbService recipeDb, TestCrud test, TagCrud tag)
        {
            this.rDbService = recipeDb;
            this.tCrud = test;
            this.tagService = tag;
            //recipes = rHolder;
        }

        public IActionResult Index()
        {
            return View(rDbService.Get());
        }

        [HttpPost]
        public IActionResult AddRecipe()
        {
            return View();
        }

        public IActionResult GetTags(string id)
        {
            return View();
        }

        public IActionResult Sorting()
        {
            return View(tagService.GetAllTagsObjects());
        }

        public IActionResult SortingResult(string id)
        {
            var getTagId = tagService.GetTags(id);
            var recFound = rDbService.GetByTag(getTagId);
            return View(recFound);
        }

        [HttpPost]
        public IActionResult TagTest()
        {

            return View("~/Views/Home/Index.cshtml");
        }

        public IActionResult AddTags(string recId, int id = 0)
        {
            TagHolder t = new TagHolder();
            if (id == 0)
            {
                t.tagHolder = tagService.GetTags();
                return View(t);
            }
            else
            {
                t.tagHolder = tagService.GetTags();
                t.tagMarkedHolder = tagService.GetNameTags(rDbService.GetTags(recId));
                return View(t);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NewRecipe(RecipeDb recDb)
        {

            if (ModelState.IsValid)
            {
                rDbService.Create(recDb);
                return RedirectToAction(nameof(Index));
            }

            return View("AddRecipe", recDb);
        }

        public IActionResult OCRResult(IFormFile file)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            path = Path.Combine(path, "tessdata");
            path = path.Replace("file:\\", "");
            if (file == null || file.Length == 0)
            {
                ViewBag.Result = true;
                ViewBag.res = "File not found";
                return View("OCR");
            }

            using (var engine = new TesseractEngine(path, "dan", EngineMode.Default))
            {
                using (var image = new Bitmap(file.OpenReadStream()))
                {
                    using (var pix = PixConverter.ToPix(image))
                    {
                        using (var page = engine.Process(pix))
                        {
                            ViewBag.Result = true;
                            ViewBag.res = page.GetText();
                            ViewBag.mean = String.Format("{0:p}", page.GetMeanConfidence());
                            return View("OCR");
                        }
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRecToUser(RecipeDb rDb)
        {
            if (ModelState.IsValid)
            {
                rDb.Id = GetRandomHexNumber(24);
                tCrud.AddRecipe(rDb);
                return View("Index", tCrud.GetRecipes());
            }

            return View("AddRecipe", rDb);
        }

        public IActionResult Recipe(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = rDbService.Get(id);

            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        public IActionResult Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recipe = tCrud.GetRecipe(id); ;
            if (recipe == null)
            {
                return NotFound();
            }

            return View(recipe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditResult(string id, RecipeDb rDb, string save)
        {
            if (id != rDb.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                switch (save)
                {
                    case "Save Recipe":
                        tCrud.Update(rDb, id);
                        break;

                    case "Save as new":
                        rDb.Id = GetRandomHexNumber(24);
                        tCrud.AddRecipe(rDb);
                        break;
                }
                // rDbService.Update(id, rDb);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(rDb);
            }
        }

        private static string GetRandomHexNumber(int hex)
        {
            Random random = new Random();
            byte[] buffer = new byte[hex / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (hex % 2 == 0)
            {
                return result;
            }
            return result + random.Next(7).ToString("X");
        }
    }

}