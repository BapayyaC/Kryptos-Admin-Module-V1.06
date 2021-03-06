﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Kryptos.Models;
using Newtonsoft.Json;
using System.Data;

namespace Kryptos.Controllers
{
    public class LoginController : Controller
    {
        //
        // GET: /Login/
        kryptoEntities1 _context = new kryptoEntities1();
        public ActionResult Login()
        {
            Session["OrgAdmin"] = null;
            Session["FacAdmin"] = null;
            return View();
        }

        public ActionResult View2()
        {
            return View();
        }

        public static UserLoginInformation ActiveUser
        {
            get { return (UserLoginInformation)System.Web.HttpContext.Current.Session["ACTIVEUSER"]; }
            set
            {
                System.Web.HttpContext.Current.Session["ACTIVEUSER"] = value;
                if (value != null)
                {
                    if (value.Facility.SessionTimeout != null)
                    {
                        System.Web.HttpContext.Current.Session.Timeout = (int)(value.Facility.SessionTimeout);
                        ActiveUser_SESSIONID = System.Web.HttpContext.Current.Session.SessionID;
                    }
                }
            }
        }


        public static String DomainName = System.Web.HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
        public static String ActiveUser_SESSIONID { get; private set; }


        [HttpPost]
        public ActionResult LoginCredentials(UserLoginInformation obj)
        {
            Encryption eny = new Encryption();
            string EncrptPassword = eny.EncryptString(obj.Password);
            var user = _context.UserLoginInformations.FirstOrDefault(x => x.EmailId.ToLower().Equals(obj.EmailId.ToLower()) && x.Password.ToLower().Equals(EncrptPassword.ToLower()) && x.IsActive == true && x.Status == 1);
          
            if (user != null)
            {
                if (!user.IsSuperAdmin )
                {
                    if(!user.IsOrganisationAdmin)
                    {
                        if (!user.IsFacilityAdmin)
                        {
                            TempData["errormsg"] = "You are not authorised to login, Please contact Administrator";
                            return RedirectToAction("Login", "Login");
                        }
                    }
                }
                ActiveUser = user;
              
                UserRegitrationForInitialLogin IntialLogin = _context.UserRegitrationForInitialLogins.FirstOrDefault(L => L.USERID.Equals(user.USERID));
                if (IntialLogin != null)
                {
                    if (IntialLogin.IsInitialLogin)
                        return RedirectToAction("ResetPassword", "Login");
                    else
                        return RedirectToAction("List", "UserDatatables");
                }
                else
                    return RedirectToAction("List", "UserDatatables");
            }
            
            TempData["errormsg"] = "Please Enter Correct Username And Password";
            return RedirectToAction("Login", "Login");
        }

        public ActionResult Logout()
        {
            ActiveUser = null;
            TempData["ErrorMessage"] = "Successfully Logged Out";
            return RedirectToAction("Login", "Login");
        }

        public int SetResetPassword(string oldPassword, int userid, string newpassword)
        {
            var user = _context.UserLoginInformations.Find(userid);
            Encryption eny = new Encryption();
            string decryptPassword = eny.DecryptStringAES(user.Password);
            if (oldPassword != decryptPassword) return 2;
            string EncrptPassword = eny.EncryptString(newpassword);
            user.Password = EncrptPassword;
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
            return 1;
        }


        public ActionResult ResetPassword()
        {
            return View();
        }
   
        public ActionResult ResetPasswordForUser(string Password)
        {
            UserLoginInformation loggedinUser = (UserLoginInformation)LoginController.ActiveUser;
            Encryption eny = new Encryption();
            string EncrptPassword = eny.EncryptString(Password);
            loggedinUser.Password = EncrptPassword;
            _context.Entry(loggedinUser).State = EntityState.Modified;
            _context.SaveChanges();
            UserRegitrationForInitialLogin IntialLogin = _context.UserRegitrationForInitialLogins.SingleOrDefault(In => In.USERID.Equals(loggedinUser.USERID));
            IntialLogin.IsInitialLogin = false;
            _context.Entry(IntialLogin).State = EntityState.Modified;
            _context.SaveChanges();
            return Json(new { result = "Redirect", url = Url.Action("List", "UserDatatables") });
        }
    }

}

