using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net.Mail;
using System.Data.SqlClient;
using UserAccountsModel;

namespace AppReporting
{
    public partial class _Default : System.Web.UI.Page
    {
        MediaAxEntities data;
        UserAccountsEntities accounts;
        string currentApp = "";
        string cookieName = "LoggedInAs";
        protected void Page_Load(object sender, EventArgs e)
        {
            data = new MediaAxEntities("metadata=res://*;provider=System.Data.SqlClient;provider connection string='Data Source=mmodsql1.orcsweb.com;Initial Catalog=MediaAx;User ID=mmod_report_user;Password=\"MarchMadness;1\";MultipleActiveResultSets=True;Network Library=dbmssocn'");
            data.CommandTimeout = 300;

            //If there isn't a user logged in yet, I'll need the accounts database.
            if (loginDiv.Visible)
            {
                accounts = new UserAccountsEntities("metadata=res://*;provider=System.Data.SqlClient;provider connection string='Data Source=mmodsql1.orcsweb.com;Initial Catalog=UserAccounts;User ID=mmod_report_user;Password=\"MarchMadness;1\";MultipleActiveResultSets=True;Network Library=dbmssocn'");
                accounts.CommandTimeout = 300;
            }

            if (!IsPostBack)
            {

                //set properties for the main datagrid.
                myDataGrid.AutoGenerateColumns = true;
                myDataGrid.BorderWidth = 2;
                myDataGrid.CellSpacing = 1;
                myDataGrid.GridLines = GridLines.Both;
                myDataGrid.BackColor = System.Drawing.Color.Beige;
                myDataGrid.HeaderStyle.BackColor = System.Drawing.Color.BurlyWood;
                myDataGrid.AlternatingItemStyle.BackColor = System.Drawing.Color.Gainsboro;


                var browsers = from item in data.LogBrowsers
                               orderby item.LogBrowserId
                               select new
                               {
                                   ID = item.LogBrowserId,
                                   Name = item.LogBrowserName,
                                   Version = item.LogBrowserVersion
                               };
                var browserNames = (from item in browsers select item.Name).Distinct();

                //Populate the TreeView
                TreeNode root = new TreeNode(currentApp);
                TreeNode osNode = new TreeNode("Operating Systems");
                TreeNode browserNode = new TreeNode("Browsers");
                foreach (string br in browserNames)
                {
                    TreeNode tempNode = new TreeNode(br);
                    tempNode.SelectAction = TreeNodeSelectAction.Select;
                    browserNode.ChildNodes.Add(tempNode);
                }
                browserNode.Expanded = false;
                var osNames = from item in data.LogPlatforms select item.LogPlatformName;
                foreach (string os in osNames)
                {
                    TreeNode tempNode = new TreeNode(os);
                    tempNode.SelectAction = TreeNodeSelectAction.Select;
                    osNode.ChildNodes.Add(tempNode);
                }
                foreach (TreeNode node in browserNode.ChildNodes)
                {
                    TreeNode OSes = new TreeNode("Operating Systems");
                    foreach (string os in osNames)
                    {
                        TreeNode tempNode = new TreeNode(os);
                        tempNode.SelectAction = TreeNodeSelectAction.Select;
                        OSes.ChildNodes.Add(tempNode);
                    }
                    OSes.Expanded = false;
                    node.ChildNodes.Add(OSes);
                    node.Expanded = false;
                }
                foreach (TreeNode node in osNode.ChildNodes)
                {
                    TreeNode brs = new TreeNode("Browsers");
                    foreach (string br in browserNames)
                    {
                        TreeNode tempNode = new TreeNode(br);
                        tempNode.SelectAction = TreeNodeSelectAction.Select;
                        brs.ChildNodes.Add(tempNode);
                    }
                    brs.Expanded = false;
                    node.ChildNodes.Add(brs);
                    node.Expanded = false;
                }
                osNode.Expanded = false;
                root.ChildNodes.Add(osNode);
                root.ChildNodes.Add(browserNode);
                root.Selected = true;
                SearchTree.Nodes.Add(root);

                //Check the cookie for auto-login
                string username = ReadCookie();
                if (username != null)
                {
                    try
                    {
                        User user = accounts.User.First(x => x.UserName == username);
                        LoginSuccessful(user);
                    }
                    catch
                    {
                    }
                }

                StartCalendar.SelectedDate = DateTime.Today;
                EndCalendar.SelectedDate = DateTime.Today;
            }
            
            //Anytime something is clicked, we hit this on post-back.
            if (!loginDiv.Visible)
            {
                //Change the app at the root of the tree view to match the one chosen in the dropdown.
                if (currentApp != AppIDList.SelectedItem.Text)
                {
                    SearchTree.Nodes[0].Value = AppIDList.SelectedItem.Value;
                    SearchTree.Nodes[0].Text = AppIDList.SelectedItem.Text;
                    Submitted(sender, e);
                }
                //Enable the calendars for custom dates.
                if (TimeSelection.SelectedIndex == 4)
                {
                    StartCalendar.Enabled = true;
                    EndCalendar.Enabled = true;
                }
                else
                {
                    StartCalendar.Enabled = false;
                    EndCalendar.Enabled = false;
                }
                //Submitted(sender, e);
            }
        }

        //Given the user's app permissions, fill out the dropdown.
        protected void PopulateAppMenu(List<int> appIds)
        {
            AppIDList.Items.Clear();
            foreach (int id in appIds)
            {
                LogApplications app = data.LogApplications.First(x => x.LogApplicationId == id);
                AppIDList.Items.Add(new ListItem(app.LogApplicationName, app.LogApplicationId.ToString()));
            }
            AppIDList.SelectedIndex = 0;
            currentApp = AppIDList.SelectedItem.Text;
            SearchTree.Nodes[0].Text = currentApp;
            SearchTree.Nodes[0].Value = AppIDList.SelectedItem.Value;
        }

        //Creates a Stack representing the path walked down the tree to the chosen node.
        //The string from the root node is at the top of the stack, and the string from
        //the chosen node is down at the bottom of the stack.
        protected Stack<string> ParseTreeChoice()
        {
            Stack<string> rootDown = new Stack<string>();
            TreeNode tempNode = SearchTree.SelectedNode;
            string path = tempNode.Text;
            rootDown.Push(tempNode.Text);
            tempNode = tempNode.Parent;
            while (tempNode != null)
            {
                rootDown.Push(tempNode.Text);
                path = tempNode.Text + " - " + path;
                tempNode = tempNode.Parent;
            }
            DebugLabel.Text = path;
            return rootDown;
        }

        //I pull off the choice stack, building a Linq query as I go.  First I narrow by app,
        //then by date (if applicable), then by the choice made by the user.  When I reach
        //the decision the user has made, and have whittled down the results accordingly,
        //I make the actual select statement, building out the grid as necessary for the
        //user's choice.  After all of that, all that remains is to databind the grid.
        //The Selects at the end are ugly if you choose OSes or Browsers and get the spread;
        //I just haven't yet found a better way to do it.
        protected void Submitted(object sender, EventArgs e)
        {
            //Olympics times out, so don't bother trying.
            if (AppIDList.SelectedItem.Value == "0")
            {
                /*
                //This was to test the time required to query just the count of one state in the Olympics dataset.
                var count = (from log in data.Log
                             where ((log.LogApplicationId == 1) &&
                                    (log.InstSilverlightStates.InstSilverlightStateId == 1))
                             select log).Count();
                //countLabel.Text = count.ToString();
                //data.Connection.Close();
                //data.Dispose();
                 */
                return;
            }
            //Get the chosen node and its parents, all the way up to the root.
            Stack<string> rootDown = ParseTreeChoice();

            //Get the root, filter by that app.
            string app = rootDown.Pop();
            var whittle = from item in data.Log
                          where ((item.LogApplications.LogApplicationName == app) &&
                                 (item.InstSilverlightStates.InstSilverlightStateId != null))
                          select item;

            //Further whittle down the selection by the appropriate time selection.
            //Custom time selection is inclusive on both ends.
            int timeChoice = TimeSelection.SelectedIndex;
            DateTime startDate = DateTime.Today.Date;
            DateTime endDate = DateTime.Today.Date.AddDays(1);
            if (timeChoice != 0)
            {
                switch (timeChoice)
                {
                    //Past 6 months
                    case 1:
                        startDate = endDate.Subtract(TimeSpan.FromDays(180));
                        break;
                    //Past month
                    case 2:
                        startDate = endDate.Subtract(TimeSpan.FromDays(30));
                        break;
                    //Past week
                    case 3:
                        startDate = endDate.Subtract(TimeSpan.FromDays(7));
                        break;
                    //Custom dates
                    case 4:
                        startDate = StartCalendar.SelectedDate.Date;
                        endDate = EndCalendar.SelectedDate.Date;
                        //Switch the dates around if the Start date is later than the End date.
                        if (endDate < startDate)
                        {
                            DateTime tempDate = startDate;
                            startDate = endDate;
                            endDate = tempDate;
                        }
                        //Add a day, so that it's midnight the morning after, instead of the day of.
                        endDate = endDate.AddDays(1);
                        //Custom dates are the only time there's an end date to consider, so do
                        //the whittling here and spare a larger sql query for other requests.
                        whittle = from item in whittle
                                  where item.LogCreated < endDate
                                  select item;
                        break;
                }
                whittle = from item in whittle
                          where item.LogCreated >= startDate
                          select item;
            }

            //The user chose the root node on the tree view, so we display totals for the app.
            if (rootDown.Count == 0)
            {
                var totals = from log in whittle
                             group log by log.InstSilverlightStates into states
                             orderby states.Key.InstSilverlightStateId
                             select new
                             {
                                 ID = states.Key.InstSilverlightStateId,
                                 State = states.Key.InstSilverlightState,
                                 Total = states.Count()
                             };
                myDataGrid.DataSource = totals;
            }
            else
            {
                string primary = rootDown.Pop();

                if (primary == "Operating Systems")
                {

                    //User Clicked on app/OS, display a breakdown of all of them.
                    if (rootDown.Count == 0)
                    {
                        var OSTotals = from log in whittle
                                       group log by log.InstSilverlightStates into states
                                       orderby states.Key.InstSilverlightStateId
                                       select new
                                       {
                                           ID = states.Key.InstSilverlightStateId,
                                           State = states.Key.InstSilverlightState,
                                           Total = states.Count(),
                                           WinXP = (from item in states
                                                    where item.LogPlatformId == 1
                                                    select item).Count(),
                                           Unknown = (from item in states
                                                      where item.LogPlatformId == 2
                                                      select item).Count(),
                                           WinNT = (from item in states
                                                    where item.LogPlatformId == 3
                                                    select item).Count(),
                                           Win2000 = (from item in states
                                                      where item.LogPlatformId == 4
                                                      select item).Count(),
                                           Win98 = (from item in states
                                                    where item.LogPlatformId == 5
                                                    select item).Count(),
                                           MacPPC = (from item in states
                                                     where item.LogPlatformId == 6
                                                     select item).Count(),
                                           UNIX = (from item in states
                                                   where item.LogPlatformId == 7
                                                   select item).Count(),
                                           WinCE = (from item in states
                                                    where item.LogPlatformId == 8
                                                    select item).Count(),
                                           Win95 = (from item in states
                                                    where item.LogPlatformId == 9
                                                    select item).Count(),
                                           Mac68k = (from item in states
                                                     where item.LogPlatformId == 10
                                                     select item).Count()
                                       };
                        myDataGrid.DataSource = OSTotals;
                    }
                    else
                    {
                        string os = rootDown.Pop();
                        whittle = from item in whittle
                                  where item.LogPlatforms.LogPlatformName == os
                                  select item;

                        //User chose app/OS/single.  Display data.
                        if (rootDown.Count == 0)
                        {
                            var OSTotal = from item in whittle
                                          group item.InstSilverlightStates by item.InstSilverlightStates into states
                                          orderby states.Key.InstSilverlightStateId
                                          select new
                                          {
                                              ID = states.Key.InstSilverlightStateId,
                                              State = states.Key.InstSilverlightState,
                                              Total = states.Count()
                                          };
                            myDataGrid.DataSource = OSTotal;
                        }
                        else
                        {
                            rootDown.Pop();     //This was "Browsers"

                            //User chose app/OS/single/browsers.  Display Data.
                            if (rootDown.Count == 0)
                            {
                                var browserBreakdown = from log in whittle
                                                       group log by log.InstSilverlightStates into states
                                                       orderby states.Key.InstSilverlightStateId
                                                       select new
                                                       {
                                                           ID = states.Key.InstSilverlightStateId,
                                                           State = states.Key.InstSilverlightState,
                                                           Total = states.Count(),
                                                           IE = (from item in states
                                                                 where item.LogBrowsers.LogBrowserName == "IE"
                                                                 select item).Count(),
                                                           Firefox = (from item in states
                                                                      where item.LogBrowsers.LogBrowserName == "Firefox"
                                                                      select item).Count(),
                                                           Safari = (from item in states
                                                                     where item.LogBrowsers.LogBrowserName == "AppleMAC-Safari"
                                                                     select item).Count(),
                                                           Opera = (from item in states
                                                                    where item.LogBrowsers.LogBrowserName == "Opera"
                                                                    select item).Count(),
                                                           Mozilla = (from item in states
                                                                      where item.LogBrowsers.LogBrowserName == "Mozilla"
                                                                      select item).Count(),
                                                           Ericsson = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Ericsson"
                                                                       select item).Count(),
                                                           Firebird = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Firebird"
                                                                       select item).Count(),
                                                           imode = (from item in states
                                                                    where item.LogBrowsers.LogBrowserName == "i-mode"
                                                                    select item).Count(),
                                                           MSIE = (from item in states
                                                                   where item.LogBrowsers.LogBrowserName == "MSIE"
                                                                   select item).Count(),
                                                           NetFront = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Compact NetFront"
                                                                       select item).Count(),
                                                           Netscape = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Netscape"
                                                                       select item).Count(),
                                                           Nokia = (from item in states
                                                                    where item.LogBrowsers.LogBrowserName == "Nokia"
                                                                    select item).Count(),
                                                           Phonecom = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Phone.com"
                                                                       select item).Count(),
                                                           PocketIE = (from item in states
                                                                       where item.LogBrowsers.LogBrowserName == "Pocket IE"
                                                                       select item).Count(),
                                                           SonyEricsson = (from item in states
                                                                           where item.LogBrowsers.LogBrowserName == "Sony Ericsson"
                                                                           select item).Count(),
                                                           Unknown = (from item in states
                                                                      where item.LogBrowsers.LogBrowserName == "Unknown"
                                                                      select item).Count(),
                                                           WinCE = (from item in states
                                                                    where item.LogBrowsers.LogBrowserName == "WinCE"
                                                                    select item).Count()
                                                       };
                                myDataGrid.DataSource = browserBreakdown;
                            }

                            //User chose app/OS/single/browsers/single.  Display for that browser.
                            else
                            {
                                string browser = rootDown.Pop();
                                whittle = from item in whittle
                                          where item.LogBrowsers.LogBrowserName == browser
                                          select item;
                                var singleBrowser = from item in whittle
                                                    group item by item.InstSilverlightStates into states
                                                    orderby states.Key.InstSilverlightStateId
                                                    select new
                                                    {
                                                        ID = states.Key.InstSilverlightStateId,
                                                        State = states.Key.InstSilverlightState,
                                                        Total = states.Count()
                                                    };
                                myDataGrid.DataSource = singleBrowser;
                            }
                        }
                    }
                }
                else if (primary == "Browsers")
                {
                    //User chose app/Browsers.  Display data for all of them.
                    if (rootDown.Count == 0)
                    {
                        var browserBreakdown = from log in whittle
                                               group log by log.InstSilverlightStates into states
                                               orderby states.Key.InstSilverlightStateId
                                               select new
                                               {
                                                   ID = states.Key.InstSilverlightStateId,
                                                   State = states.Key.InstSilverlightState,
                                                   Total = states.Count(),
                                                   IE = (from item in states
                                                         where item.LogBrowsers.LogBrowserName == "IE"
                                                         select item).Count(),
                                                   Firefox = (from item in states
                                                              where item.LogBrowsers.LogBrowserName == "Firefox"
                                                              select item).Count(),
                                                   Safari = (from item in states
                                                             where item.LogBrowsers.LogBrowserName == "AppleMAC-Safari"
                                                             select item).Count(),
                                                   Opera = (from item in states
                                                            where item.LogBrowsers.LogBrowserName == "Opera"
                                                            select item).Count(),
                                                   Mozilla = (from item in states
                                                              where item.LogBrowsers.LogBrowserName == "Mozilla"
                                                              select item).Count(),
                                                   Ericsson = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Ericsson"
                                                               select item).Count(),
                                                   Firebird = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Firebird"
                                                               select item).Count(),
                                                   imode = (from item in states
                                                            where item.LogBrowsers.LogBrowserName == "i-mode"
                                                            select item).Count(),
                                                   MSIE = (from item in states
                                                           where item.LogBrowsers.LogBrowserName == "MSIE"
                                                           select item).Count(),
                                                   NetFront = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Compact NetFront"
                                                               select item).Count(),
                                                   Netscape = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Netscape"
                                                               select item).Count(),
                                                   Nokia = (from item in states
                                                            where item.LogBrowsers.LogBrowserName == "Nokia"
                                                            select item).Count(),
                                                   Phonecom = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Phone.com"
                                                               select item).Count(),
                                                   PocketIE = (from item in states
                                                               where item.LogBrowsers.LogBrowserName == "Pocket IE"
                                                               select item).Count(),
                                                   SonyEricsson = (from item in states
                                                                   where item.LogBrowsers.LogBrowserName == "Sony Ericsson"
                                                                   select item).Count(),
                                                   Unknown = (from item in states
                                                              where item.LogBrowsers.LogBrowserName == "Unknown"
                                                              select item).Count(),
                                                   WinCE = (from item in states
                                                            where item.LogBrowsers.LogBrowserName == "WinCE"
                                                            select item).Count()
                                               };
                        myDataGrid.DataSource = browserBreakdown;
                    }
                    else
                    {
                        string br = rootDown.Pop();
                        whittle = from item in whittle
                                  where item.LogBrowsers.LogBrowserName == br
                                  select item;

                        //User chose app/Browsers/single.  Display data.
                        if (rootDown.Count == 0)
                        {
                            var browserTotal = from item in whittle
                                               group item.InstSilverlightStates by item.InstSilverlightStates into states
                                               orderby states.Key.InstSilverlightStateId
                                               select new
                                               {
                                                   ID = states.Key.InstSilverlightStateId,
                                                   State = states.Key.InstSilverlightState,
                                                   Total = states.Count()
                                               };
                            myDataGrid.DataSource = browserTotal;
                        }
                        else
                        {
                            rootDown.Pop();     //This was "Operating Systems"

                            //User chose app/Browsers/single/OS.  Display data.
                            if (rootDown.Count == 0)
                            {
                                var OSTotals = from log in whittle
                                               group log by log.InstSilverlightStates into states
                                               orderby states.Key.InstSilverlightStateId
                                               select new
                                               {
                                                   ID = states.Key.InstSilverlightStateId,
                                                   State = states.Key.InstSilverlightState,
                                                   Total = states.Count(),
                                                   WinXP = (from item in states
                                                            where item.LogPlatformId == 1
                                                            select item).Count(),
                                                   Unknown = (from item in states
                                                              where item.LogPlatformId == 2
                                                              select item).Count(),
                                                   WinNT = (from item in states
                                                            where item.LogPlatformId == 3
                                                            select item).Count(),
                                                   Win2000 = (from item in states
                                                              where item.LogPlatformId == 4
                                                              select item).Count(),
                                                   Win98 = (from item in states
                                                            where item.LogPlatformId == 5
                                                            select item).Count(),
                                                   MacPPC = (from item in states
                                                             where item.LogPlatformId == 6
                                                             select item).Count(),
                                                   UNIX = (from item in states
                                                           where item.LogPlatformId == 7
                                                           select item).Count(),
                                                   WinCE = (from item in states
                                                            where item.LogPlatformId == 8
                                                            select item).Count(),
                                                   Win95 = (from item in states
                                                            where item.LogPlatformId == 9
                                                            select item).Count(),
                                                   Mac68k = (from item in states
                                                             where item.LogPlatformId == 10
                                                             select item).Count()
                                               };
                                myDataGrid.DataSource = OSTotals;
                            }

                            //User chose app/Browsers/single/OS/single
                            else
                            {
                                string os = rootDown.Pop();
                                whittle = from item in whittle
                                          where item.LogPlatforms.LogPlatformName == os
                                          select item;
                                var OSTotal = from item in whittle
                                              group item.InstSilverlightStates by item.InstSilverlightStates into states
                                              orderby states.Key.InstSilverlightStateId
                                              select new
                                              {
                                                  ID = states.Key.InstSilverlightStateId,
                                                  State = states.Key.InstSilverlightState,
                                                  Total = states.Count()
                                              };
                                myDataGrid.DataSource = OSTotal;
                            }
                        }
                    }
                }
            }
            try
            {
                countLabel.Text = whittle.Count().ToString();
                myDataGrid.DataBind();
            }
            catch
            {
                DebugLabel.Text = "Request timed out.";
            }
            RegularGrid.Visible = true;
            myDataGrid.Visible = true;
        }

        protected void RefreshGrid(object sender, EventArgs e)
        {
            Submitted(sender, e);
        }

        //Try to log in, can pass or fail
        //The login logic is mostly just copy/pasted from Manage.aspx, where I wrote it first.
        protected void AttemptLogin(object sender, EventArgs e)
        {
            loginDebug.Text = "";
            try
            {
                User user = accounts.User.First(x => x.UserName == loginUsername.Text);
                if (user.Password == loginPassword.Text)
                {
                    LoginSuccessful(user);
                }
                else
                {
                    LoginUnsuccessful();
                }
            }
            //First() throws an exception instead of returning null.
            //Rather than querying twice with Any() then First(), I just catch
            //the exception from First().
            catch
            {
                LoginUnsuccessful();
            }
        }

        //Password matches, so let the user in.
        protected void LoginSuccessful(User user)
        {
            List<int> appIds = new List<int>();
            
            //Admins have full access.
            if (user.IsAdmin)
            {
                appIds.AddRange(from item in accounts.Application select item.LogApplicationId);
            }

            //Non-admins only get their assigned apps.
            else
            {
                user.Application.Load();
                var ids = from item in user.Application
                          orderby item.LogApplicationId
                          select item.LogApplicationId;
                if (ids.Count() == 0)
                {
                    loginDebug.Text = "The account " + user.UserName + " does not have any permissions assigned.";
                    return;
                }
                appIds.AddRange(ids);
            }
            PopulateAppMenu(appIds);

            loggedInAsLabel.Text = user.UserName;
            loginUsername.Text = "";
            loginPassword.Text = "";
            loginDiv.Visible = false;
            appDataDiv.Visible = true;
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
            appDataDiv.Visible = false;
            loginDiv.Visible = true;
            loginDebug.Text = "Logged Out.";
            ClearCookie();
            ClearData();
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

        //Empties the data when the user logs out.
        protected void ClearData()
        {
            RegularGrid.Visible = false;
            TimeSelection.SelectedIndex = 0;
        }
    }
}
