<%@ page language="C#" autoeventwireup="true" inherits="AppReporting._Default, App_Web_cercsorh" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Reporting</title>
    <style type="text/css"> 
body {
    background: white;
    color: #0a0a0a;
    font-family: Segoe UI, Verdana, Arial, Helvetica, sans-serif;
    margin-top: 20px;
}
</style>

</head>
<body>
    <form id="form1" runat="server">
    <!--
         Displayed on first entry unless the logged-in cookie has been set.
     -->
    <div id="loginDiv" runat="server">
        Username: <asp:TextBox ID="loginUsername" runat="server"></asp:TextBox>
        <br />
        Password: <asp:TextBox ID="loginPassword" TextMode="Password" runat="server"></asp:TextBox>
        <br />
        <asp:CheckBox ID="CookieCheckBox" Text="Stay Logged in. (1 week)" runat="server" />
        <br />
        <asp:Button ID="Button4" UseSubmitBehavior="true" OnClick="AttemptLogin" Text="Log In" runat="server" />
        <asp:Label ID="loginDebug" runat="server"></asp:Label>
    </div>
    
    <!--
         Displayed once the user is logged in.
     -->
    <div id="appDataDiv" visible="false" runat="server">
        Currently logged in as: <b><asp:Label ID="loggedInAsLabel" runat="server"></asp:Label></b> <asp:Button ID="Button5" Text="Logout" OnClick="LogOut" runat="server" />
        <br />
        Application:
        <asp:DropDownList ID="AppIDList" AutoPostBack="true" runat="server" />
        <br />
        <table style="background-color:#F0F0FF;border-width:1px;border-style:solid;border-color:Black">
            <tr align="center">
                <td>Time Frame:</td>
                <td>Start Date</td>
                <td>End Date</td>
            </tr>
            <tr>
                <td>
                    <asp:RadioButtonList ID="TimeSelection" AutoPostBack="true" runat="server">
                        <asp:ListItem Selected="True" Text="All Entries" />
                        <asp:ListItem Text="Past Six Months (180 days)" />
                        <asp:ListItem Text="Past Month (30 days)" />
                        <asp:ListItem Text="Past Week" />
                        <asp:ListItem Text="Custom Dates" />
                    </asp:RadioButtonList>
                </td>
                <td>
                    <asp:Calendar ID="StartCalendar" OnSelectionChanged="Submitted" Enabled="false" runat="server" />
                </td>
                <td>
                    <asp:Calendar ID="EndCalendar" OnSelectionChanged="Submitted" Enabled="false" runat="server" />
                </td>
            </tr>
        </table>
        <asp:TreeView ID="SearchTree" OnSelectedNodeChanged="Submitted" runat="server"></asp:TreeView>
        <div id="RegularGrid" visible="false" runat="server">
            <asp:Label ID="DebugLabel" runat="server" />
            <br />
            Combined Total: <asp:Label ID="countLabel" runat="server" />
            <asp:DataGrid ID="myDataGrid" runat="server" />
        </div>
    </div>
    </form>
</body>
</html>
