using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;  //Here for the pretty file dialogs.  Cosmetic choice only.
using System.Xml.Linq;
using System.IO;

namespace BracketMaker
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private List<MMODGame> games;           //a list of the games, in tournament order.
        const int TOTAL = 64;                   //total number of games.
        int gamesCompleted;
        Dictionary<string, Team> teams;         //Dictionary to hold the information for the teams.
        List<Channel> channels;                 //List to hold thhe Channels for each day.
        List<string> videos;                    //strings to construct video ids from.
        string urlRoot = "http://build.mmod2010.vertigo.com/DataSources/custom/";   //Intended online root directory for the brackets.
        string url;                             //This will be the online directory where the bracket is intended to sit.

        //strings representing game states.
        const string INPROGRESS = "INPROGRESS";
        const string FINAL = "FINAL";
        const string SCHEDULED = "SCHEDULED";
        const string HALFTIME = "HALFTIME";
        Random rand;                            //Random generator to give semi-interesting numbers for the xml files I write.
        int videoNum = 0;                       //Used to rotate through the available video IDs so they're not all the same.

        //Init for various items
        public Window1()
        {
            InitializeComponent();
            games = new List<MMODGame>(64);
            for (int i = 0; i < TOTAL; i++)
            {
                games.Add(new MMODGame());
            }
            gamesCompleted = 0;
            teams = new Dictionary<string, Team>();
            InitTeamList();
            channels = new List<Channel>(13);
            rand = new Random();
            InitChannelData();
            videos = new List<string>();
            InitVideoList();
        }

        //Stick something in the textbox and load the bracket from it's seed file.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            infoPane.Text = "Program Started";
            LoadBracketInfoFromFile("BracketSeed.xml");
        }

        //Validates and updates scores when the user clicks out of a score box.
        private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            System.Windows.Controls.TextBox box = sender as System.Windows.Controls.TextBox;
            StackPanel gamePanel = (box.Parent as StackPanel).Parent as StackPanel;
            ValidateScore(box);
            PropagateWinner(gamePanel, false);
        }

        //Validates and updates scores when the user clicks out of a score box.
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox box = sender as System.Windows.Controls.TextBox;
            StackPanel gamePanel = (box.Parent as StackPanel).Parent as StackPanel;
            ValidateScore(box);
            PropagateWinner(gamePanel, false);
        }

        //Makes sure the score entered in a box is a valid number, sets to zero if not.
        private void ValidateScore(System.Windows.Controls.TextBox box)
        {
            try
            {
                int temp = int.Parse(box.Text);
                if (temp < 0)
                    box.Text = "0";
            }
            catch
            {
                box.Text = "0";
            }
        }

        //Load the bracket file when the user clicks Load.
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        //Load the bracket file and attempt to extract a custom online directory
        //from a matching (in the same directory) settings.xml
        private void LoadFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            ofd.FileName = "Bracket.xml";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadBracketInfoFromFile(ofd.FileName);
                string localPath = ofd.FileName.Substring(0, ofd.FileName.LastIndexOf('\\') + 1);   //Folder where the bracket file lives.
                string settingsFile = localPath + "Settings.xml";
                if (File.Exists(settingsFile))
                {
                    string onlineDirectory = ExtractURLFromFile(localPath + "Settings.xml");
                    urlDirectory.Text = onlineDirectory;
                }
            }
        }

        //Save Bracket.xml and related files when the user says so.
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        //The user has the option to select a target online directory in a pre-
        //designated location, or to override with a fully customized url.
        private void SetURL()
        {
            if ((bool)UrlOverride.IsChecked)
            {
                url = UrlOverrideBox.Text;
                if (!url.EndsWith("/"))
                {
                    url += "/";
                }
            }
            else
            {
                url = urlRoot;
                if (urlDirectory.Text != "")
                    url = url + urlDirectory.Text + "/";
            }
        }

        //Craft and save all relevant files for the bracket.  A file dialog is opened,
        //but only the directory is used -- the filename itself is ignored.
        private void SaveFile()
        {
            SetURL();
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            sfd.FileName = "Bracket.xml";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string localPath = sfd.FileName.Substring(0, sfd.FileName.LastIndexOf('\\') + 1);

                infoPane.Text = "Saving Bracket Data to " + localPath + "...";
                MoveAllDisplayToData();
                XElement bracketXml = CraftBracketFile();
                bracketXml.Save(localPath + "Bracket.xml");
                infoPane.Text += "\nSuccessfully saved Bracket.xml";
                XElement channelXml = CraftChannelsXmlFile();
                channelXml.Save(localPath + "Channels.xml");
                infoPane.Text += "\nSuccessfully saved Channels.xml";
                XDocument settings = CraftSettingsXmlFile();
                settings.Save(localPath + "Settings.xml");
                infoPane.Text += "\nSuccessfully saved Settings.xml";
                CreateAndSaveChannelFiles(localPath + "Channels\\");
                infoPane.Text += "\nSuccessfully saved channel files";
            }
        }

        //Loads the chosen file into an XDocument, performs a very simple format check,
        //then attempts to parse and display the data.
        private void LoadBracketInfoFromFile(string filename)
        {
            infoPane.Text = "";
            XDocument bracketfile = XDocument.Load(filename);
            IEnumerable<XElement> xmlGames = bracketfile.Descendants("game");
            IEnumerable<XElement> OpeningRound = bracketfile.Descendants("opening_game");
            xmlGames = OpeningRound.Concat(xmlGames);
            if (xmlGames.Count() != 64)
            {
                infoPane.Text = "Incorrect number of games in Bracket file: " + xmlGames.Count().ToString();
                return;
            }
            LoadXmlToData(xmlGames);
            MoveAllDataToDisplay();
            if (infoPane.Text == "")
            {
                infoPane.Text += "\nBracket file loaded Successfully.";
            }
        }

        //Looks for a custom url in a file expected to be settings.xml.
        //If the url looks like one from this tool, then it updates the
        //appropriate textbox to reflect it.
        private string ExtractURLFromFile(string fileName)
        {
            string directory = urlDirectory.Text;
            XDocument settings = XDocument.Load(fileName);
            IEnumerable<XElement> sources = settings.Descendants("DataSource");
            foreach (XElement el in sources)
            {
                if (el.Attribute("Name").Value == "SettingsSource")
                {
                    string settingsUrl = el.Attribute("Url").Value;
                    if (settingsUrl.StartsWith(urlRoot))
                    {
                        directory = settingsUrl.Substring(urlRoot.Length, settingsUrl.LastIndexOf('/') - urlRoot.Length);
                        break;
                    }
                }
            }
            return directory;
        }

        //Loads bracket data from XElements extracted from the xml file to the
        //List<MMODGame> where they're stored in memory.
        private void LoadXmlToData(IEnumerable<XElement> xmlGames)
        {
            MMODGame tempGame = new MMODGame();
            int gameNumber = 0;
            //Games are assumed to be in order, since the numbers won't necessarily be.
            foreach (XElement game in xmlGames)
            {
                XElement home = game.Element("hometeam");
                XElement away = game.Element("awayteam");

                //Rather than hard-code all the teams with their information, I take them from the bracket.xml file as I load it.
                if (!teams.ContainsKey(home.Value))
                {
                    teams.Add(home.Value, new Team(home.Value, home.Attribute("abbr").Value, int.Parse(home.Attribute("id").Value), int.Parse(home.Attribute("seed").Value)));
                }
                if (!teams.ContainsKey(away.Value))
                {
                    teams.Add(away.Value, new Team(away.Value, away.Attribute("abbr").Value, int.Parse(away.Attribute("id").Value), int.Parse(away.Attribute("seed").Value)));
                }

                try { tempGame.HomeScore = int.Parse(home.Attribute("score").Value); }
                catch { tempGame.HomeScore = 0; }
                try { tempGame.AwayScore = int.Parse(away.Attribute("score").Value); }
                catch { tempGame.AwayScore = 0; }
                tempGame.HomeTeam = home.Value;
                tempGame.AwayTeam = away.Value;
                tempGame.State = game.Attribute("status").Value.ToUpper();
                tempGame.ID = int.Parse(game.Attribute("id").Value);

                games[gameNumber].Clone(tempGame);
                gameNumber++;
            }
        }

        //English friendly middle-method.
        private void MoveAllDataToDisplay()
        {
            MoveBetweenDataAndDisplay(true);
        }

        //English friendly middle-method.
        private void MoveAllDisplayToData()
        {
            MoveBetweenDataAndDisplay(false);
        }

        //Copies game information from memory to the graphical display or the other
        //way around.  
        private void MoveBetweenDataAndDisplay(bool toDisplay)
        {
            gamesCompleted = 0;
            int i = 0;
            StackPanel TargetGame;
            while (i < 64)
            {
                //Graphically, the games exist as StackPanels, housing the different elements.
                //Each is named gameXX to correspond with the game it represents.
                TargetGame = FinalFourGroup.FindName("game" + i) as StackPanel;
                if (toDisplay)
                {
                    CopyDataToDisplay(i, TargetGame);
                }
                else
                {
                    CopyDisplayToData(TargetGame, i);
                }
                i++;
            }
            //Flips a radio button to show that games have started.
            if (gamesCompleted > 0)
            {
                GamesStarted.IsChecked = true;
            }
        }

        //Move a given game from memory to the matching graphic elements.
        private void CopyDataToDisplay(int gameNumber, StackPanel displayGroup)
        {
            MMODGame tempGame = games[gameNumber];
            UIElementCollection members = displayGroup.Children;
            (members[5] as System.Windows.Controls.CheckBox).IsChecked = false;
            if (tempGame.State == FINAL)
            {
                (members[2] as System.Windows.Controls.RadioButton).IsChecked = true;
                gamesCompleted++;
            }
            else if (tempGame.State == INPROGRESS)
            {
                (members[1] as System.Windows.Controls.RadioButton).IsChecked = true;
            }
            else if (tempGame.State == HALFTIME)
            {
                (members[1] as System.Windows.Controls.RadioButton).IsChecked = true;
                (members[5] as System.Windows.Controls.CheckBox).IsChecked = true;
            }
            else
            {
                (members[0] as System.Windows.Controls.RadioButton).IsChecked = true;
            }
            ((members[4] as StackPanel).Children[0] as System.Windows.Controls.TextBox).Text = tempGame.HomeScore.ToString();
            ((members[4] as StackPanel).Children[1] as TextBlock).Text = tempGame.HomeTeam;
            ((members[3] as StackPanel).Children[0] as System.Windows.Controls.TextBox).Text = tempGame.AwayScore.ToString();
            ((members[3] as StackPanel).Children[1] as TextBlock).Text = tempGame.AwayTeam;
        }

        //Move a given game from the graphical display to memory.
        //This method pulls the values from each graphical element of the game's
        //StackPanel and stores the data in memory for easier access.
        private void CopyDisplayToData(StackPanel displayGroup, int gameNumber)
        {
            UIElementCollection members = displayGroup.Children;
            System.Windows.Controls.RadioButton scheduled = members[0] as System.Windows.Controls.RadioButton;
            System.Windows.Controls.RadioButton inProgress = members[1] as System.Windows.Controls.RadioButton;
            System.Windows.Controls.RadioButton final = members[2] as System.Windows.Controls.RadioButton;
            System.Windows.Controls.CheckBox halftime = members[5] as System.Windows.Controls.CheckBox;
            System.Windows.Controls.TextBox homeScore = (members[4] as StackPanel).Children[0] as System.Windows.Controls.TextBox;
            TextBlock homeTeam = (members[4] as StackPanel).Children[1] as TextBlock;
            System.Windows.Controls.TextBox awayScore = (members[3] as StackPanel).Children[0] as System.Windows.Controls.TextBox;
            TextBlock awayTeam = (members[3] as StackPanel).Children[1] as TextBlock;

            MMODGame tempGame = new MMODGame();
            if ((bool)scheduled.IsChecked)
            {
                tempGame.State = SCHEDULED;
            }
            else if ((bool)inProgress.IsChecked)
            {
                tempGame.State = INPROGRESS;
                if ((bool)halftime.IsChecked)
                {
                    tempGame.State = HALFTIME;
                }
            }
            else
            {
                tempGame.State = FINAL;
                gamesCompleted++;
            }
            tempGame.HomeScore = int.Parse(homeScore.Text);
            tempGame.HomeTeam = homeTeam.Text;
            tempGame.AwayScore = int.Parse(awayScore.Text);
            tempGame.AwayTeam = awayTeam.Text;
            tempGame.ID = games[gameNumber].ID;
            games[gameNumber].Clone(tempGame);
        }

        //This actually makes the Bracket.xml file, using helper methods to construct the nodes.
        //It attempts to give realistic-looking data so there's something interesting to look at
        //when you open it up in the player.
        private XElement CraftBracketFile()
        {
            XElement ncaa = XElement.Parse("<ncaab_brackets xmlns:fn=\"http://www.w3.org/2005/02/xpath-functions\" xmlns:ns0=\"http://xml.cbssports.com/schema/bracket001\" season=\"2010\" tournament=\"Division I Men's\"/>");
            ncaa.Add(AddGameInfoToXml(new XElement("opening_game"), 0, true));

            //Round 1
            XElement round1 = new XElement("round");
            round1.SetAttributeValue("name", "First Round");
            round1.SetAttributeValue("start", "03/19/09");
            round1.SetAttributeValue("end", "03/20/09");
            AddFourRegionsToRound(round1, 1, 8);
            round1.SetAttributeValue("status", DeriveRoundStatus(1, 32));
            ncaa.Add(round1);

            //Round 2
            XElement round2 = new XElement("round");
            round2.SetAttributeValue("name", "Second Round");
            round2.SetAttributeValue("start", "03/21/09");
            round2.SetAttributeValue("end", "03/22/09");
            round2.SetAttributeValue("status", DeriveRoundStatus(33, 48));
            AddFourRegionsToRound(round2, 33, 4);
            ncaa.Add(round2);

            //Sweet Sixteen
            XElement ss = new XElement("round");
            ss.SetAttributeValue("name", "Regionals");
            ss.SetAttributeValue("start", "03/26/09");
            ss.SetAttributeValue("end", "03/27/09");
            ss.SetAttributeValue("status", DeriveRoundStatus(49, 56));
            AddFourRegionsToRound(ss, 49, 2);
            ncaa.Add(ss);

            //Elite Eight
            XElement ee = new XElement("round");
            ee.SetAttributeValue("name", "Regionals");
            ee.SetAttributeValue("start", "03/28/09");
            ee.SetAttributeValue("end", "03/29/09");
            ee.SetAttributeValue("status", DeriveRoundStatus(57, 60));
            AddFourRegionsToRound(ee, 57, 1);
            ncaa.Add(ee);

            //FinalFour
            XElement ff = new XElement("round");
            ff.SetAttributeValue("name", "Semifinals");
            ff.SetAttributeValue("start", "04/04/09");
            ff.SetAttributeValue("end", "04/04/09");
            ff.SetAttributeValue("status", DeriveRoundStatus(61, 62));
            XElement ffRegion = new XElement("region");
            ffRegion.SetAttributeValue("name", "FinalFour");
            ffRegion.SetAttributeValue("id", "5");
            ffRegion.Add(AddGameInfoToXml(new XElement("game"), 61, true));
            ffRegion.Add(AddGameInfoToXml(new XElement("game"), 62, true));
            ff.Add(ffRegion);
            ncaa.Add(ff);

            //Championship
            XElement ch = new XElement("round");
            ch.SetAttributeValue("name", "National Championship");
            ch.SetAttributeValue("start", "04/06/09");
            ch.SetAttributeValue("end", "04/06/09");
            ch.SetAttributeValue("status", DeriveRoundStatus(63, 63));
            XElement chRegion = new XElement("region");
            chRegion.SetAttributeValue("name", "FinalFour");
            chRegion.SetAttributeValue("id", "5");
            chRegion.Add(AddGameInfoToXml(new XElement("game"), 63, true));
            ch.Add(chRegion);
            ncaa.Add(ch);

            return ncaa;
        }

        //Walks through a round's games and finds what state I think it should be
        //labeled as.
        private string DeriveRoundStatus(int startID, int endID)
        {
            int scheduled = 0;
            int final = 0;
            int inP = 0;
            for (int id = startID; id <= endID; id++)
            {
                string state = games[id].State;
                switch (state)
                {
                    case SCHEDULED:
                        scheduled++;
                        break;
                    case FINAL:
                        final++;
                        break;
                    case INPROGRESS:
                        inP++;
                        break;
                    case HALFTIME:
                        inP++;
                        break;
                }
            }

            //If any games are in progress, so is the round.
            if (inP > 0)
            {
                return INPROGRESS;
            }
            //If all games are final, so is the round.
            else if (final == (endID - startID + 1))
            {
                return FINAL;
            }
            //Otherwise, the round is still scheduled.
            else
            {
                return SCHEDULED;
            }
        }

        //Add four regions, with accompanying games, to a round.
        //Caller decides which games are added.
        private void AddFourRegionsToRound(XElement round, int firstGameID, int gamesPerRegion)
        {
            XElement eastRegion = new XElement("region");
            eastRegion.SetAttributeValue("id", "3");
            eastRegion.SetAttributeValue("name", "East");

            XElement midwestRegion = new XElement("region");
            midwestRegion.SetAttributeValue("id", "1");
            midwestRegion.SetAttributeValue("name", "Midwest");

            XElement southRegion = new XElement("region");
            southRegion.SetAttributeValue("id", "4");
            southRegion.SetAttributeValue("name", "South");

            XElement westRegion = new XElement("region");
            westRegion.SetAttributeValue("id", "2");
            westRegion.SetAttributeValue("name", "West");

            for (int id = firstGameID; id < (firstGameID + gamesPerRegion); id++)
            {
                eastRegion.Add(AddGameInfoToXml(new XElement("game"), id, true));
                midwestRegion.Add(AddGameInfoToXml(new XElement("game"), id + gamesPerRegion, true));
                southRegion.Add(AddGameInfoToXml(new XElement("game"), id + (gamesPerRegion * 2), true));
                westRegion.Add(AddGameInfoToXml(new XElement("game"), id + (gamesPerRegion * 3), true));
            }
            round.Add(eastRegion);
            round.Add(midwestRegion);
            round.Add(southRegion);
            round.Add(westRegion);
        }

        //This method constructs the xml nodes inside the regions, representing
        //the different games.  This uses some static data, like the location, but
        //most is dynamically created to give interesting results for when you open
        //the bracket up in the viewer.
        //The xml format is different between bracket.xml and the channel.xml files,
        //so I've included a bool to let this one method handle both.  Many strings
        //will change depending on that bool, and some elements and attributes will
        //be changed as well.
        //It's messy, and I'm sorry.
        private XElement AddGameInfoToXml(XElement el, int id, bool forBracket)
        {
            int homeScore = games[id].HomeScore;
            string homeTeam = games[id].HomeTeam;
            int awayScore = games[id].AwayScore;
            string awayTeam = games[id].AwayTeam;
            string state = games[id].State;

            string abbrString = "abbr";
            string timeString = "start_time";
            string statusString = "status";
            string periodString = "Period";
            string progressValue = "";
            string winnerID = "";
            if (!forBracket)
            {
                abbrString = "game_abbr";
                timeString = "est_time";
                statusString = "event_status";
                periodString = "event_period";
            }
            el.SetAttributeValue("id", games[id].ID);
            DateTime now = DateTime.Now;
            now += TimeSpan.FromMinutes(rand.NextDouble() * 180);
            el.SetAttributeValue(abbrString, "NCAAB_" + now.ToString("yyyyMMdd") + "_" + teams[awayTeam].abbr + "@" + teams[homeTeam].abbr);
            string timeValue = now.ToString("MM/dd/yy hh:mmtt") + " EST";
            if (forBracket)
            {
                timeValue = now.ToString("MM/dd/yy HH:mm") + " ET";
            }
            el.SetAttributeValue(timeString, timeValue);
            if (forBracket)
            {
                el.SetAttributeValue("location", "Dayton, Ohio");
                if (state == HALFTIME)
                {
                    state = INPROGRESS;
                }
            }
            el.SetAttributeValue(statusString, state);
            bool final = (state == FINAL);
            bool started = (state != SCHEDULED);
            int period = 0;
            if (final)
            {
                period = 2;
                progressValue = "0";
                if (homeScore >= awayScore)
                {
                    winnerID = teams[homeTeam].id.ToString();
                }
                else
                {
                    winnerID = teams[awayTeam].id.ToString();
                }
            }
            else if (started)
            {
                period = rand.Next(1, 3);
                TimeSpan time = TimeSpan.FromSeconds(rand.Next(0, 1200));
                progressValue = time.Minutes + ":" + String.Format("{0:00}", time.Seconds);
            }
            if (!forBracket)
            {
                el.SetAttributeValue("game_time", CalculateCurrentESTUnixTime(now));
                el.SetAttributeValue("event_progress", progressValue);
                if (started)
                {
                    el.SetAttributeValue("winner_id", winnerID);
                }
            }
            if (started)
                el.SetAttributeValue(periodString, period);

            XElement venue = new XElement("venue");
            venue.SetValue("Hinkle Fieldhouse");
            el.Add(venue);
            if (!forBracket)
            {
                XElement title = new XElement("title");
                title.SetValue(homeTeam + " vs. " + awayTeam);
                el.Add(title);

                XElement desc = new XElement("description");
                desc.SetValue(teams[awayTeam].abbr + " takes on rank " + teams[homeTeam].seed + " " + teams[homeTeam].abbr + " on home turf.");
                el.Add(desc);

                venue.SetAttributeValue("id", "547196");
            }

            //used to craft the actual file names.
            //If you opened a real bracket file, and have gameIDs that were not
            //generated by me (mine are 0-64), then it will use the real gameID.
            //Otherwise, it uses one of a set of video IDs read from a file.
            //
            //It is important to rotate the videos linked to, because the "now watching"
            //state is driven by the video you're watching, not the game that you clicked on.
            //As such when you click on one game, all games linked to the same video are set as
            //"now watching" and it's not useful for testing.
            string mediaBase = videos[videoNum];
            videoNum = (videoNum + 1) % videos.Count;
            if (games[id].ID > 64)
            {
                mediaBase = games[id].ID.ToString();
            }

            XElement gv = new XElement("gamevideo");
            gv.SetAttributeValue("id", mediaBase + "0");
            gv.SetAttributeValue("available", started ? "True" : "False");
            gv.SetAttributeValue("isLive", (started && !final) ? "True" : "False");
            el.Add(gv);

            XElement ga = new XElement("fullgameaudio");
            ga.SetAttributeValue("id", mediaBase + "1");
            ga.SetAttributeValue("available", started ? "True" : "False");
            ga.SetAttributeValue("isLive", (started && !final) ? "True" : "False");
            el.Add(ga);

            XElement gh = new XElement("gamehighlights");
            gh.SetAttributeValue("id", mediaBase + "2");
            gh.SetAttributeValue("available", final ? "True" : "False");
            gh.SetAttributeValue("isLive", "False");
            el.Add(gh);

            XElement nb = new XElement("nailbiter");
            nb.SetAttributeValue("id", mediaBase + "3");
            nb.SetAttributeValue("available", final ? "True" : "False");
            nb.SetAttributeValue("isLive", "False");
            el.Add(nb);

            //This part crafts the hometeam element.
            string elHomeTeam = "hometeam";
            if (!forBracket)
            {
                elHomeTeam = "home_team";
            }
            XElement ht = new XElement(elHomeTeam);
            ht.SetAttributeValue("id", teams[homeTeam].id);
            ht.SetAttributeValue("abbr", teams[homeTeam].abbr);
            ht.SetAttributeValue("score", homeScore);
            ht.SetAttributeValue("seed", teams[homeTeam].seed);
            if (forBracket)
            {
                ht.SetAttributeValue("winner", (final && (homeScore > awayScore)) ? "Yes" : "No");
                ht.SetAttributeValue("Logo", "http://logourl");
                ht.SetValue(homeTeam);
            }
            else
            {
                ht.SetAttributeValue("location", homeTeam);
                ht.SetAttributeValue("nickname", "");
            }
            el.Add(ht);

            //The awayteam element is crafted here.
            string elAwayTeam = "awayteam";
            if (!forBracket)
            {
                elAwayTeam = "away_team";
            }
            XElement awt = new XElement(elAwayTeam);
            awt.SetAttributeValue("id", teams[awayTeam].id);
            awt.SetAttributeValue("abbr", teams[awayTeam].abbr);
            awt.SetAttributeValue("score", awayScore);
            awt.SetAttributeValue("seed", teams[awayTeam].seed);
            if (forBracket)
            {
                awt.SetAttributeValue("winner", (final && (awayScore > homeScore)) ? "Yes" : "No");
                awt.SetAttributeValue("Logo", "http://logourl");
                awt.SetValue(awayTeam);
            }
            else
            {
                awt.SetAttributeValue("location", awayTeam);
                awt.SetAttributeValue("nickname", "");
            }
            el.Add(awt);

            return el;
        }

        //This just gives me an easy way to convert .net time to Unix time,
        //which is used in the xml files.
        private int CalculateCurrentESTUnixTime(DateTime time)
        {
            //Sets my EST 0 mark for Unix time
            DateTime originTime = DateTime.Parse("12/31/1969 7:00:00 PM");
            int unixTime = (int)time.Subtract(originTime).TotalSeconds;
            return unixTime;
        }

        //Initially I hard-coded these in because I didn't want to write the code
        //to parse them from the bracket file.  I later wrote the code, because
        //some of the values were different, so then I only
        //needed a seed entry in here to handle blank entries.
        private void InitTeamList()
        {
            /*
            teams.Add("Alabama St.", new Team("Alabama St.", "ALST", 21387, 16));
            teams.Add("Morehead St.", new Team("Morehead St.", "MOREHDST", 21329, 16));
            teams.Add("Pittsburgh", new Team("Pittsburgh", "PITT", 21165, 1));
            teams.Add("E. Tenn. St.", new Team("E. Tenn. St.", "ETNST", 21369, 16));
            teams.Add("Oklahoma St.", new Team("Oklahoma St.", "OKST", 21209, 8));
            teams.Add("Tennessee", new Team("Tennessee", "TN", 21362, 9));
            teams.Add("Florida St.", new Team("Florida St.", "FLST", 21131, 5));
            teams.Add("Wisconsin", new Team("Wisconsin", "WI", 21200, 12));
            teams.Add("Xavier", new Team("Xavier", "XAVIER", 21149, 4));
            teams.Add("Portland St.", new Team("Portland St.", "PORTST", 21181, 13));
            teams.Add("UCLA", new Team("UCLA", "UCLA", 21341, 6));
            teams.Add("VCU", new Team("VCU", "VACOMM", 21232, 11));
            teams.Add("Villanova", new Team("Villanova", "NOVA", 21171, 3));
            teams.Add("American", new Team("American", "AMER", 21225, 14));
            teams.Add("Texas", new Team("Texas", "TX", 21210, 7));
            teams.Add("Minnesota", new Team("Minnesota", "MN", 21195, 10));
            teams.Add("Duke", new Team("Duke", "DUKE", 21130, 2));
            teams.Add("Binghamton", new Team("Binghamton", "BING", 160867, 15));
            teams.Add("Louisville", new Team("Louisville", "LOU", 21238, 1));
            teams.Add("Ohio St.", new Team("Ohio St.", "OHST", 21197, 8));
            teams.Add("Siena", new Team("Siena", "SIE", 21262, 9));
            teams.Add("Arizona", new Team("Arizona", "AZ", 21335, 12));
            teams.Add("Utah", new Team("Utah", "UT", 21437, 5));
            teams.Add("Wake Forest", new Team("Wake Forest", "WF", 21137, 4));
            teams.Add("Cleveland St.", new Team("Cleveland St.", "CLEVST", 21297, 13));
            teams.Add("West Virginia", new Team("West Virginia", "WV", 21172, 6));
            teams.Add("Dayton", new Team("Dayton", "DAY", 21138, 11));
            teams.Add("Kansas", new Team("Kansas", "KS", 21204, 3));
            teams.Add("N. Dakota St.", new Team("N. Dakota St.", "NDS", 160983, 14));
            teams.Add("Boston Coll.", new Team("Boston Coll.", "BC", 21160, 7));
            teams.Add("Southern Cal.", new Team("Southern Cal.", "USC", 21342, 10));
            teams.Add("Michigan St.", new Team("Michigan St.", "MIST", 21194, 2));
            teams.Add("Robert Morris", new Team("Robert Morris", "ROB", 21319, 15));
            teams.Add("North Carolina", new Team("North Carolina", "NC", 21134, 1));
            teams.Add("Radford", new Team("Radford", "RADFRD", 21186, 16));
            teams.Add("LSU", new Team("LSU", "LSU", 21358, 8));
            teams.Add("Butler", new Team("Butler", "BUT", 21296, 9));
            teams.Add("Illinois", new Team("Illinois", "IL", 21190, 5));
            teams.Add("Western Ky.", new Team("Western Ky.", "WKTY", 21403, 12));
            teams.Add("Gonzaga", new Team("Gonzaga", "GONZAG", 21415, 4));
            teams.Add("Akron", new Team("Akron", "AKR", 21264, 13));
            teams.Add("Arizona St.", new Team("Arizona St.", "AZST", 21336, 6));
            teams.Add("Temple", new Team("Temple", "TEMP", 21147, 11));
            teams.Add("Syracuse", new Team("Syracuse", "SYR", 21170, 3));
            teams.Add("S. F. Austin", new Team("S. F. Austin", "SFA", 21383, 14));
            teams.Add("Clemson", new Team("Clemson", "CLEM", 21129, 7));
            teams.Add("Michigan", new Team("Michigan", "MI", 21193, 10));
            teams.Add("Oklahoma", new Team("Oklahoma", "OK", 21208, 2));
            teams.Add("Morgan St.", new Team("Morgan St.", "MRGNST", 21291, 15));
            teams.Add("Connecticut", new Team("Connecticut", "CT", 21161, 1));
            teams.Add("Chattanooga", new Team("Chattanooga", "TNCHAT", 21366, 16));
            teams.Add("BYU", new Team("BYU", "BYU", 21424, 8));
            teams.Add("Texas A&M", new Team("Texas A&M", "TXAM", 21211, 9));
            teams.Add("Purdue", new Team("Purdue", "PUR", 21199, 5));
            teams.Add("Northern Iowa", new Team("Northern Iowa", "NIA", 21310, 12));
            teams.Add("Washington", new Team("Washington", "WA", 21343, 4));
            teams.Add("Mississippi St.", new Team("Mississippi St.", "MSST", 21360, 13));
            teams.Add("Marquette", new Team("Marquette", "MARQET", 21239, 6));
            teams.Add("Utah St.", new Team("Utah St.", "UTST", 21224, 11));
            teams.Add("Missouri", new Team("Missouri", "MO", 21206, 3));
            teams.Add("Cornell", new Team("Cornell", "CORN", 21248, 14));
            teams.Add("California", new Team("California", "CA", 21337, 7));
            teams.Add("Maryland", new Team("Maryland", "MD", 21133, 10));
            teams.Add("Memphis", new Team("Memphis", "MEM", 21240, 2));
            teams.Add("CS Northridge", new Team("CS Northridge", "CSNTH", 21177, 15));
            */
            teams.Add("", new Team("", "", 0, 0));
        }

        //Rather than parse them from a channels.xml seed file, I hard coded these, allowing me
        //to dictate the names of the files I'd be writing.
        private void InitChannelData()
        {
                channels.Add(new Channel("Historical Highlights", "HistoricalHighlights_Channel.xml", "HistoricalHighlights", 0));
                channels.Add(new Channel("Selection Show Sunday, March 14", "SelectionShow_Channel.xml", "SelectionSunday", 0));
                channels.Add(new Channel("Opening Round Tuesday, March 16", "OpeningGame_Channel.xml", "OpeningGame", 0));
                channels.Add(new Channel("First Round Thursday, March 18", "Round1_Day1_Channel.xml", "RegionalGames", 1));
                channels.Add(new Channel("First Round Friday, March 19", "Round1_Day2_Channel.xml", "RegionalGames", 17));
                channels.Add(new Channel("Second Round Saturday, March 20", "Round2_Day1_Channel.xml", "RegionalGames", 33));
                channels.Add(new Channel("Second Round Sunday, March 21", "Round2_Day2_Channel.xml", "RegionalGames", 41));
                channels.Add(new Channel("Sweet Sixteen Thursday, March 25", "SweetSixteen_Day1_Channel.xml", "RegionalGames", 49));
                channels.Add(new Channel("Sweet Sixteen Friday, March 26", "SweetSixteen_Day2_Channel.xml", "RegionalGames", 53));
                channels.Add(new Channel("Regional Finals Saturday, March 27", "EliteEight_Day1_Channel.xml", "SemifinalGames", 57));
                channels.Add(new Channel("Regional Finals Sunday, March 28", "EliteEight_Day2_Channel.xml", "SemifinalGames", 59));
                channels.Add(new Channel("Final Four Saturday, April 3", "Semifinal_Channel.xml", "SemifinalGames", 61));
                channels.Add(new Channel("National Championship Monday, April 5", "Finals_Channel.xml", "ChampionshipGame", 63));
        }

        //Kicks off creation of Channels.xml
        private XElement CraftChannelsXmlFile()
        {
            //13 channels total.
            XElement chnls = new XElement("channels");
            for (int i = 0; i < 13; i++)
            {
                chnls.Add(CraftChannelXmlNode(i));
            }
            return chnls;
        }

        //Creates the channel node, setting it to available if it should be.
        private XElement CraftChannelXmlNode(int chanNum)
        {
            string available = "True";
            if ((bool)PreSelectionShow.IsChecked)
            {
                if (chanNum > 0)
                    available = "False";
            }
            else if (gamesCompleted < channels[chanNum].reqLvl)
            {
                available = "False";
            }
            XElement node = new XElement("channel");
            node.SetAttributeValue("available", available);
            node.Add(XElement.Parse("<name><![CDATA[" + channels[chanNum].cdata + "]]></name>"));
            node.Add(XElement.Parse("<datasource>" + url + "Channels/" + channels[chanNum].file + "</datasource>"));
            node.Add(XElement.Parse("<channeltype>" + channels[chanNum].type + "</channeltype>"));
            return node;
        }

        //This loads an existing Settings.xml file and changes the urls for the
        //other key xml files.  This way, we could place all the files online
        //and people would only need to hijack Settings.xml in Fiddler and it
        //would already point to the rest of the files, hosted online.  The trouble then was that
        //the Settings.xml file was frequently edited, and then all the preset states
        //that we had placed online had to be updated with the new settings file or else
        //they'd break the player.
        private XDocument CraftSettingsXmlFile()
        {
            XDocument settings = XDocument.Load("SettingsSeed.xml");
            IEnumerable<XElement> sources = settings.Descendants("DataSource");
            foreach (XElement el in sources)
            {
                switch (el.Attribute("Name").Value)
                {
                    case "SettingsSource":
                        el.SetAttributeValue("Url", url + "settings.xml");
                        break;
                    case "ChannelsSource":
                        el.SetAttributeValue("Url", url + "Channels.xml");
                        break;
                    case "BracketSource":
                        el.SetAttributeValue("Url", url + "Bracket.xml");
                        break;
                }
            }
            return settings;
        }

        //This oversees the creation of each individual channel.xml file in .\channels\
        private void CreateAndSaveChannelFiles(string localPath)
        {
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            //Historical Highlights, I copy it straight over.
            XDocument.Load("HHSeed.xml").Save(localPath + channels[0].file);
            if (!(bool)PreSelectionShow.IsChecked)
            {
                //I do a straight copy of the Selection Show channel as well.
                XDocument.Load("SSSeed.xml").Save(localPath + channels[1].file);

                //Walk through the other channels and create their xml files.
                for (int id = 2; id < channels.Count; id++)
                {
                    string fileNameFull = localPath + channels[id].file;
                    //Only bother with a channel if enough games are completed.
                    if (gamesCompleted >= channels[id].reqLvl)
                    {
                        int next = 63;  //Default for the last channel
                        if (id < 12)
                        {
                            next = channels[id + 1].reqLvl - 1;
                        }
                        CraftAndSaveChannelFile(fileNameFull, channels[id].reqLvl, next);
                    }
                    //If the channel will not be made, make sure it's not there.
                    else
                    {
                        File.Delete(fileNameFull);
                    }
                }
            }
        }

        //This creates a given channel file and saves it to disk.
        private void CraftAndSaveChannelFile(string fileNameFull, int gameIDStart, int gameIDEnd)
        {
            XElement channel = new XElement("scores");
            channel.SetAttributeValue("date", DateTime.Now.ToString("dd/MM/yyyy"));
            channel.SetAttributeValue("time", DateTime.Now.ToString("HH:mm:ss") + " EST");
            XElement gms = new XElement("games");
            for (int id = gameIDStart; id <= gameIDEnd; id++)
            {
                gms.Add(AddGameInfoToXml(new XElement("game"), id, false));
            }
            channel.Add(gms);
            channel.Save(fileNameFull);
        }

        //When you set a game as final, this populates the next game with the winner.
        private void RadioButton_Final_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.RadioButton rb = sender as System.Windows.Controls.RadioButton;
            PropagateWinner(rb.Parent as StackPanel, false);
        }

        //emptyWinner tells us if we're emptying the next game's slot.
        //We do that when Final is unchecked for a game.
        //In case of a tie, I assign the home team as the winner.
        private void PropagateWinner(StackPanel currentGame, bool emptyWinner)
        {
            
            System.Windows.Controls.RadioButton final = currentGame.Children[2] as System.Windows.Controls.RadioButton;
            if (!(bool)final.IsChecked)
            {
                emptyWinner = true;
            }
            
            string gameName = currentGame.Name;
            int gameID = int.Parse(gameName.Substring(4));  //Substring(4) skips "game" and just gets the number.
            if (gameID == 63)  //Last game, nowhere to propagate to.
            {
                return;
            }
            int nextGameID = FindNextGameID(gameID);
            string nextGameName = "game" + nextGameID;
            StackPanel nextGame = currentGame.FindName(nextGameName) as StackPanel;

            //Find the slot in the next game where the current game's winner goes.
            StackPanel winnerInNextGame = nextGame.Children[4] as StackPanel;
            if ((gameID % 2) == 0) //if it's even numbered, it moves up as the Away team instead.
            {
                winnerInNextGame = nextGame.Children[3] as StackPanel;
            }
            
            //if we're clearing, just do it and move on.
            if (emptyWinner)
            {
                (winnerInNextGame.Children[1] as TextBlock).Text = "";
                if (gameID == 0)
                {
                    (winnerInNextGame.Children[1] as TextBlock).Text = "Op. Rd. Winner";
                }
                PropagateWinner(nextGame, true);
                return;
            }

            StackPanel currentHomePanel = currentGame.Children[4] as StackPanel;
            StackPanel currentAwayPanel = currentGame.Children[3] as StackPanel;
            string homeTeamName = (currentHomePanel.Children[1] as TextBlock).Text;
            string awayTeamName = (currentAwayPanel.Children[1] as TextBlock).Text;
            if ((homeTeamName == "") || (awayTeamName == "") || (awayTeamName == "Op. Rd. Winner"))  //Missing a team from current game.
            {
                return;
            }
            int homeScore = int.Parse((currentHomePanel.Children[0] as System.Windows.Controls.TextBox).Text);
            int awayScore = int.Parse((currentAwayPanel.Children[0] as System.Windows.Controls.TextBox).Text);

            string winner = homeTeamName; //default to Home team as winner in case of a tie.
            if (awayScore > homeScore)
            {
                winner = awayTeamName;
            }

            (winnerInNextGame.Children[1] as TextBlock).Text = winner;
            PropagateWinner(nextGame, false);
        }

        //This calculates the ID of the next game for the current game's winner.
        //Only intended for games before the championship, IDs 0-62.
        private int FindNextGameID(int currentGameID)
        {
            if (currentGameID == 0)
                return 17;
            if (currentGameID >= 63)
                return 63;
            return ((63 + currentGameID + 2) / 2);
        }

        //If you unchecked Final, this clears the winner from subsequent games.
        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.RadioButton rb = sender as System.Windows.Controls.RadioButton;
            PropagateWinner(rb.Parent as StackPanel, false);
        }

        //Enables using Enter to save the Bracket.
        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveFile();
            }
        }

        //Enables the custom url box if urlOverride is checked.
        private void UrlOverride_Checked(object sender, RoutedEventArgs e)
        {
            urlDirectory.IsEnabled = false;
            UrlOverrideBox.IsEnabled = true;
        }

        //Disables the custom url box if urlOverride is unchecked.
        private void UrlOverride_Unchecked(object sender, RoutedEventArgs e)
        {
            urlDirectory.IsEnabled = true;
            UrlOverrideBox.IsEnabled = false;
        }

        //This grabs video numbers from the xml file.
        //The list was valid when I made it, but the videos didn't always stay valid.
        private void InitVideoList()
        {
            XDocument videoFile = XDocument.Load("videos.xml");
            videos.AddRange(from XElement el in videoFile.Descendants("video")
                            select el.Value);
        }
    }

    public class MMODGame
    {
        int homeScore;
        int awayScore;
        string homeTeam;
        string awayTeam;
        string state;
        int id;
        public MMODGame()
        {
            homeScore = 0;
            awayScore = 0;
            id = 0;
            homeTeam = "Home Team";
            awayTeam = "Away Team";
            state = "SCHEDULED";
        }
        public MMODGame(int hms, int awys, string hmt, string awyt, string st, int id)
        {
            homeScore = hms;
            awayScore = awys;
            homeTeam = hmt;
            awayTeam = awyt;
            state = st;
            this.id = id;
        }
        public void Clone(MMODGame input)
        {
            homeScore = input.HomeScore;
            awayScore = input.AwayScore;
            homeTeam = input.HomeTeam;
            awayTeam = input.AwayTeam;
            state = input.State;
            id = input.id;
        }
        public int HomeScore
        {
            get { return homeScore; }
            set { homeScore = value; }
        }
        public int AwayScore
        {
            get { return awayScore; }
            set { awayScore = value; }
        }
        public string HomeTeam
        {
            get { return homeTeam; }
            set { homeTeam = value; }
        }
        public string AwayTeam
        {
            get { return awayTeam; }
            set { awayTeam = value; }
        }
        public string State
        {
            get { return state; }
            set { state = value; }
        }
        public int ID
        {
            get { return id; }
            set { id = value; }
        }
    }

    public class Team
    {
        public string name;
        public string abbr;
        public int id;
        public int seed;

        public Team(string nm, string ab, int idnum, int sd)
        {
            name = nm;
            abbr = ab;
            id = idnum;
            seed = sd;
        }
    }

    public class Channel
    {
        public string cdata;
        public string file;
        public string type;
        public int reqLvl;

        public Channel(string c, string f, string t, int r)
        {
            cdata = c;
            file = f;
            type = t;
            reqLvl = r;
        }
    }
}
