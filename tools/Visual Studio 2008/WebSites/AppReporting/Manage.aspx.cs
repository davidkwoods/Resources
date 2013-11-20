using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using UserAccountsModel;

namespace AppReporting
{
    public partial class Manage : System.Web.UI.Page
    {
        UserAccountsEntities data;
        string cookieName = "LoggedInAs";

        protected void Page_Load(object sender, EventArgs e)
        {
            data = new UserAccountsEntities("metadata=res://*;provider=System.Data.SqlClient;provider connection string='Data Source=mmodsql1.orcsweb.com;Initial Catalog=UserAccounts;User ID=mmod_report_user;Password=\"MarchMadness;1\";MultipleActiveResultSets=True;Network Library=dbmssocn'");

            //If it's the user is just entering the page, attempt to log in via cookie
            if (!IsPostBack)
            {
                string username = ReadCookie();
                if (username != null)
                {
                    User user = data.User.First(x => x.UserName == username);
                    LoginSuccessful(user);
                }
            }
        }

        //Brings the database's list of apps into compliance with the official list.
        protected void RefreshDBAppList()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Updates from the official app list:<br/>");
            using (MediaAxEntities logDB = new MediaAxEntities("metadata=res://*;provider=System.Data.SqlClient;provider connection string='Data Source=mmodsql1.orcsweb.com;Initial Catalog=MediaAx;User ID=mmod_report_user;Password=\"MarchMadness;1\";MultipleActiveResultSets=True;Network Library=dbmssocn'"))
            {
                var logapps = from item in logDB.LogApplications select item;
                string action = "";
                bool adding = false;
                bool changedOne = false;
                bool changeHappened = false;

                //First, remove any local applications that don't officially exist anymore.
                var localApps = from item in data.Application
                                select item;
                foreach (Application app in localApps)
                {
                    if (logapps.Any(x => x.LogApplicationId == app.LogApplicationId))
                    {
                        continue;
                    }
                    else
                    {
                        builder.Append("Removed nonexistent Application: ");
                        builder.Append(app.LogApplicationId + " " + app.LogApplicationName + " " + app.LogApplicationVersion + "<br/>");
                        data.DeleteObject(app);
                        changeHappened = true;
                    }
                }

                //Then, add or edit applications where the local copy doesn't match the official version.
                foreach (LogApplications logApp in logapps)
                {
                    adding = false;
                    changedOne = false;
                    Application app;
                    try
                    {
                        app = data.Application.First(x => x.LogApplicationId == logApp.LogApplicationId);
                        action = "Edited \"" + app.LogApplicationId + " " + app.LogApplicationName + " " + app.LogApplicationVersion + "\" to match: ";
                    }
                    catch
                    {
                        app = new Application();
                        app.LogApplicationId = logApp.LogApplicationId;
                        action = "Added new Application: ";
                        adding = true;
                        changedOne = true;
                    }
                    if (app.LogApplicationName != logApp.LogApplicationName)
                    {
                        app.LogApplicationName = logApp.LogApplicationName;
                        changedOne = true;
                    }
                    if (app.LogApplicationVersion != logApp.LogApplicationVersion)
                    {
                        app.LogApplicationVersion = logApp.LogApplicationVersion;
                        changedOne = true;
                    }
                    if (changedOne)
                    {
                        changeHappened = true;
                        builder.Append(action);
                        builder.Append(app.LogApplicationId);
                        builder.Append(" ");
                        builder.Append(app.LogApplicationName);
                        builder.Append(" ");
                        builder.Append(app.LogApplicationVersion);
                        builder.Append("<br/>");
                        if (adding)
                        {
                            data.AddToApplication(app);
                        }
                    }
                }

                //If anything was removed, added, or edited, list it.
                if (changeHappened)
                {
                    updateDebug.Text = builder.ToString();
                    data.SaveChanges();
                }
            }
        }

        //Try to log in, can pass or fail
        protected void AttemptLogin(object sender, EventArgs e)
        {
            ClearDebugLabels();
            try
            {
                User user = data.User.First(x => x.UserName == loginUsername.Text);
                if (user.Password == loginPassword.Text)
                {
                    LoginSuccessful(user);
                }
                else
                {
                    LoginUnsuccessful();
                }
            }
            //First() throws an exception rather than returning null.
            //Rather than query the database twice using Any() then First(),
            //I just catch here
            catch
            {
                LoginUnsuccessful();
            }
        }

        //Populates the User list and the Apps list (used for adding permissions)
        protected void PopulateLists()
        {
            userSelectionBox.DataSource = from item in data.User
                                          select item.UserName;
            userSelectionBox.DataBind();
            List<string> appDescriptions = new List<string>();
            foreach (Application app in data.Application)
            {
                appDescriptions.Add(app.LogApplicationId + " " + app.LogApplicationName + " " + app.LogApplicationVersion);
            }
            appListBox.DataSource = appDescriptions;
            appListBox.DataBind();

            data.Connection.Close();
            SetNewUserId();
        }
        
        //Password matches, so let the user in if he is an admin.
        protected void LoginSuccessful(User user)
        {
            if (!user.IsAdmin)
            {
                loginDebug.Text = "User <b>" + user.UserName + "</b> is not an admin.  Access denied.";
                return;
            }
            RefreshDBAppList();
            PopulateLists();
            loggedInAsLabel.Text = user.UserName;
            loginUsername.Text = "";
            loginPassword.Text = "";
            loginDiv.Visible = false;
            userDataDiv.Visible = true;
            if (CookieCheckBox.Checked)
            {
                SetCookie(user.UserName);
            }
        }

        //Give a response on login failure.
        protected void LoginUnsuccessful()
        {
            loginPassword.Text = "";
            loginPassword.Focus();
            loginDebug.Text = "Invalid name or password.";
            ClearCookie();
        }

        //Log out and say so, clearing the cookie.
        protected void LogOut(object sender, EventArgs e)
        {
            userDataDiv.Visible = false;
            loginDiv.Visible = true;
            loginDebug.Text = "Logged Out.";
            ClearCookie();
        }

        //Locate the selected user in the database, and display its data in the right fields.
        protected void DisplaySelectedUserData(object sender, EventArgs e)
        {
            User user;
            try
            {
                user = data.User.First(entry => entry.UserName == userSelectionBox.SelectedItem.Text);
            }
            catch
            {
                return;
            }
            user.Application.Load();
            userIDBox.Text = user.UserId.ToString();
            userPasswordBox.Text = user.Password;
            userAdminChoice.SelectedIndex = user.IsAdmin ? 0 : 1;

            List<string> userApps = new List<string>();
            foreach (Application app in user.Application)
            {
                userApps.Add(app.LogApplicationId + " " + app.LogApplicationName + " " + app.LogApplicationVersion);
            }
            userAppsBox.DataSource = userApps;
            userAppsBox.DataBind();
            data.Connection.Close();
            ClearDebugLabels();
        }

        //Removes the user's permission for the selected app.
        protected void DeleteAppPermission(object sender, EventArgs e)
        {
            if (userAppsBox.SelectedItem == null)
            {
                return;
            }
            int appID = int.Parse(userAppsBox.SelectedItem.Text.Split(' ')[0]);
            int userID = int.Parse(userIDBox.Text);
            User user = data.User.First(x => x.UserId == userID);
            user.Application.Load();
            Application app = user.Application.First(x => x.LogApplicationId == appID);
            user.Application.Remove(app);
            app.User.Remove(user);
            data.SaveChanges();
            DisplaySelectedUserData(sender, e);
        }

        //Gives the user permission for the selected app.
        //Does nothing if the user already has permission.
        protected void AddAppPermission(object sender, EventArgs e)
        {
            if (appListBox.SelectedItem == null)
            {
                return;
            }
            try
            {
                int appId = int.Parse(appListBox.SelectedItem.Text.Split(' ').First());
                int userId = int.Parse(userIDBox.Text);

                User user = data.User.First(x => x.UserId == userId);
                user.Application.Load();
                bool alreadyHasApp = user.Application.Any(x => x.LogApplicationId == appId);
                if (alreadyHasApp)
                {
                    return;
                }
                Application app = data.Application.First(x => x.LogApplicationId == appId);
                user.Application.Add(app);
                app.User.Add(user);
                data.SaveChanges();
                DisplaySelectedUserData(sender, e);
            }
            catch (Exception ex)
            {
                userDebug.Text = ex.ToString();
            }
        }

        //Creates a new user account.
        //Fails if the chosen name or ID is already in use.
        protected void AddNewUser(object sender, EventArgs e)
        {
            ClearDebugLabels();
            if ((addIDBox.Text == "") || (addUsernameBox.Text == ""))
            {
                addUserDebug.Text = "Cannot have empty fields.";
                return;
            }
            int id = int.Parse(addIDBox.Text);
            if (DoesUserExist(id))
            {
                addUserDebug.Text = "User already exists with that ID.";
                return;
            }
            if (DoesUserExist(addUsernameBox.Text))
            {
                addUserDebug.Text = "User already exists with that name.";
                return;
            }
            User user = new User();
            user.UserId = id;
            user.UserName = addUsernameBox.Text;
            user.Password = addPasswordBox.Text;
            user.IsAdmin = (addAdminChoice.SelectedIndex == 0);

            data.AddToUser(user);
            data.SaveChanges();
            PopulateLists();
            ClearNewUserFields();
            SelectUser(user.UserName);
            DisplaySelectedUserData(sender, e);
            addUserDebug.Text = "New user added: " + user.UserName;
        }

        //Checks the database for a user by id.
        protected bool DoesUserExist(int id)
        {
            return data.User.Any(x => x.UserId == id);
        }

        //Checks the database for a user by name.
        protected bool DoesUserExist(string name)
        {
            return data.User.Any(x => x.UserName == name);
        }

        //Deletes the selected user account.
        //Fails if you attempt to delete the account you're signed into.
        protected void RemoveUser(object sender, EventArgs e)
        {
            ClearDebugLabels();
            User user = data.User.First(x => x.UserName == userSelectionBox.SelectedItem.Text);
            if (user == null)
            {
                userDebug.Text = "Unable to find user: " + userSelectionBox.SelectedItem.Text;
                return;
            }
            if (user.UserName == loggedInAsLabel.Text)
            {
                userDebug.Text = "Cannot delete your own account.";
                return;
            }
            user.Application.Load();
            user.Application.Clear();

            data.DeleteObject(user);
            data.SaveChanges();
            ClearUserFields();
            PopulateLists();
        }

        //Saves the chosen Password and admin status for the selected user account.
        //Fails if you attempt to demote the currently logged-in admin.
        protected void UpdateUserInfo(object sender, EventArgs e)
        {
            ClearDebugLabels();
            int userID = int.Parse(userIDBox.Text);
            string username = userSelectionBox.SelectedItem.Text;
            if (userAdminChoice.SelectedIndex != 0)
            {
                if (username == loggedInAsLabel.Text)
                {
                    userDebug.Text = "Cannot set the logged-in account to user.";
                    return;
                }
            }
            User user = data.User.First(x => x.UserId == userID);
            user.IsAdmin = (userAdminChoice.SelectedIndex == 0);
            user.Password = userPasswordBox.Text;
            data.SaveChanges();
            userDebug.Text = "Account \"" + username + "\" has been updated.";
        }

        //Clears out the labels used to display messages.
        protected void ClearDebugLabels()
        {
            AppDebug.Text = addUserDebug.Text = loginDebug.Text = userDebug.Text = "";
        }

        //Clears the fields for the to-be-added user.
        protected void ClearNewUserFields()
        {
            addPasswordBox.Text = addUsernameBox.Text = "";
            addAdminChoice.SelectedIndex = 1;
            SetNewUserId();
        }

        //Populates the ID field for adding new users with a safe ID number.
        protected void SetNewUserId()
        {
            addIDBox.Text = ((from item in data.User
                              select item.UserId).Max() + 1).ToString();
        }

        //Clears the fields used to display info for the selected user.
        protected void ClearUserFields()
        {
            userIDBox.Text = userPasswordBox.Text = "";
            userAdminChoice.SelectedIndex = 1;
            userAppsBox.Items.Clear();
        }

        //Selects a given user from the list.
        protected void SelectUser(string name)
        {
            foreach (ListItem item in userSelectionBox.Items)
            {
                if (item.Text == name)
                {
                    item.Selected = true;
                    return;
                }
            }
        }

        //Sets a cookie allowing the user to stay logged in for a week.
        protected void SetCookie(string username)
        {
            Response.Cookies[cookieName].Value = username;
            Response.Cookies[cookieName].Expires = DateTime.Now.AddDays(7);
        }

        //Returns the cookie's value (the user's name) or null if there's no cookie.
        protected string ReadCookie()
        {
            if (Request.Cookies[cookieName] != null)
            {
                return Request.Cookies[cookieName].Value;
            }
            return null;
        }

        //Erases the cookie.
        protected void ClearCookie()
        {
            Response.Cookies[cookieName].Expires = DateTime.Now.AddDays(-1);
        }

        //Display's the selected app's data in the fields to the right.
        protected void DisplayAppData(object sender, EventArgs e)
        {
            if (appListBox.SelectedItem == null)
            {
                return;
            }
            int appID = int.Parse(appListBox.SelectedItem.Text.Split(' ')[0]);
            Application app = data.Application.First(x => x.LogApplicationId == appID);
            appNameBox.Text = app.LogApplicationName;
            appVersionBox.Text = app.LogApplicationVersion;
            appIDBox.Text = app.LogApplicationId.ToString();
        }

        //Adds a new Application or edits an existing one.
        protected void AddEditApplication(object sender, EventArgs e)
        {
            ClearDebugLabels();
            if ((appIDBox.Text == "") ||
                (appNameBox.Text == "") ||
                (appVersionBox.Text == ""))
            {
                AppDebug.Text = "Cannot have empty fields.";
                return;
            }
            int appID = int.Parse(appIDBox.Text);
            bool editing = false;
            Application app;
            try
            {
                app = data.Application.First(x => x.LogApplicationId == appID);
            }
            catch
            {
                app = new Application();
                app.LogApplicationId = appID;
                editing = true;
            }
            app.LogApplicationName = appNameBox.Text;
            app.LogApplicationVersion = appVersionBox.Text;
            if (editing)
            {
                data.AddToApplication(app);
            }
            data.SaveChanges();

            int user = userSelectionBox.SelectedIndex;
            PopulateLists();
            userSelectionBox.SelectedIndex = user;
            DisplaySelectedUserData(sender, e);
        }

        //Removes the indicated application.
        protected void RemoveApplication(object sender, EventArgs e)
        {
            ClearDebugLabels();
            if (appIDBox.Text == "")
            {
                AppDebug.Text = "Must choose an ID.";
                return;
            }
            int appID = -1;
            try
            {
                appID = int.Parse(appIDBox.Text);
            }
            catch
            {
                AppDebug.Text = "Must choose a valid ID.";
                return;
            }
            Application app;
            try
            {
                app = data.Application.First(x => x.LogApplicationId == appID);
            }
            catch
            {
                return;
            }
            app.User.Load();
            app.User.Clear();
            data.DeleteObject(app);
            data.SaveChanges();

            int userNum = userSelectionBox.SelectedIndex;
            PopulateLists();
            userSelectionBox.SelectedIndex = userNum;
            DisplaySelectedUserData(sender, e);
        }
    }
}
