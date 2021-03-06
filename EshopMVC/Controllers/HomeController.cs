﻿using EshopMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EshopMVC.Controllers
{
    enum OrderByEnum : int
    {
        Newest,
        Cheapest,
        MostExpensive
    }

    public class HomeController : BaseController
    {
        private readonly DB_9FCCB1_eshopEntities _db = new DB_9FCCB1_eshopEntities();

        private IEnumerable<Category> GetChildCtgrs(Category ctgr)
        {
            return _db.Category.Where(c => c.parent_id == ctgr.id);
        }

        private readonly Stack<IEnumerable<CategoryViewModel>> _stack = new Stack<IEnumerable<CategoryViewModel>>();

        private void CreateTreeModel(int ctgrId, int? previousCtgrId)
        {
            var childCtgrs = _db.Category.Where(c => c.parent_id == ctgrId).ToArray()
                .Select(c => new CategoryViewModel(c)).ToArray();
            if (previousCtgrId != null)
            {
                var prevCtgr = childCtgrs.First(c => c.Id == previousCtgrId);
                prevCtgr.Children = _stack.Peek();
            }
            _stack.Push(childCtgrs);
            if (ctgrId == 0) return;
            CreateTreeModel(_db.Category.First(c => c.id == ctgrId).parent_id, ctgrId);
        }

        private IQueryable<Product> orderQuery(IQueryable<Product> query, OrderByEnum orderType)
        {
            if (orderType == OrderByEnum.Newest)
            {
                return query.OrderBy(p => p.id);
            }
            if (orderType == OrderByEnum.Cheapest)
            {
                return query.OrderBy(p => p.price);
            }
            if (orderType == OrderByEnum.MostExpensive)
            {
                return query.OrderByDescending(p => p.price);
            }
            throw new NotImplementedException("Unknown order type.");
        }

        public ActionResult Index(int? CatId, int PageStart = 1, int OrderBy = 0)
        {
            using (_db)
            {
                var model = new CategoryBrowserViewModel();

                ViewBag.CatId = CatId;
                ViewBag.OrderBy = OrderBy;
                ViewBag.Next = PageStart + 9;
                ViewBag.Prev = Math.Max(PageStart - 9, 1);

                if (CatId == null)
                {
                    var products = _db.Product.Where(p => p.special);
                    products = orderQuery(products, (OrderByEnum) OrderBy);
                    products = products.Skip(PageStart).Take(9);

                    var topCtgrs = _db.Category.Where(c => c.parent_id == 0).ToArray();
                    model.Categories = topCtgrs.Select(c => new CategoryViewModel(c)).ToArray();
                    model.Products = products.ToArray();
                    
                    return View(model);
                }
                else
                {
                    CreateTreeModel(CatId.Value, null);
                    model.Categories = _stack.Peek();

                    List<Category> childCtgrs = _db.Category.Where(c => c.parent_id == CatId).ToList();
                    var subCtgrIds = childCtgrs.Traverse(GetChildCtgrs).ToList().Select(c => c.id);

                    var products = _db.Product.Where(
                        p => p.Category.Any(
                            c => subCtgrIds.Contains(c.id) || c.id == CatId.Value))
                            .Distinct();
                    products = orderQuery(products, (OrderByEnum) OrderBy);
                    products = products.Skip(PageStart).Take(9);

                    model.Products = products.ToList();
                }

                return View(model); 
            }
        }
	}
}