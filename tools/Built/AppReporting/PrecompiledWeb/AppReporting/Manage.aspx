<%@ page language="C#" autoeventwireup="true" inherits="AppReporting.Manage, App_Web_cercsorh" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>Management</title>
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
        <asp:Button ID="Button1" UseSubmitBehavior="true" OnClick="AttemptLogin" Text="Log In" runat="server" />
        <asp:Label ID="loginDebug" runat="server"></asp:Label>
    </div>
    
    <!--
         Displayed once the user is logged in.
     -->
    <div id="userDataDiv" visible="false" runat="server">
            Currently logged in as: <b><asp:Label ID="loggedInAsLabel" runat="server"></asp:Label></b> <asp:Button ID="Button2" Text="Logout" OnClick="LogOut" runat="server" /><br />
        <table style="background-color:#F0F0FF;border-width:1px;border-style:solid;border-color:Black">
        
        <!--
             Top-left, displays user accounts.
         -->
        <tr><td align="right">
            User:
            <asp:ListBox Width="156" ID="userSelectionBox" AutoPostBack="true" OnSelectedIndexChanged="DisplaySelectedUserData" SelectionMode="Single" runat="server"></asp:ListBox>
        </td>
        <td>
            <asp:Button ID="Button3" OnClick="RemoveUser" Text="Remove This User" runat="server" />
        </td>
        <!--
             Top-right, used to add a new user account.
             The ID box is automatically populated with a safe ID.
         -->
        <td rowspan="2" align="right" valign="top">
            <b>Add New User</b><br />
            Name:
            <asp:TextBox ID="addUsernameBox" Width="150" runat="server"></asp:TextBox>
            <br />
            Password:
            <asp:TextBox ID="addPasswordBox" Width="150" runat="server"></asp:TextBox>
            <br />
            <b>ID:</b>
            <asp:TextBox ID="addIDBox" Width="150" runat="server"></asp:TextBox>
            <br />
            <asp:RadioButtonList ID="addAdminChoice" runat="server">
                <asp:ListItem Text="Admin"></asp:ListItem>
                <asp:ListItem Selected="True" Text="Not Admin"></asp:ListItem>
            </asp:RadioButtonList>
            <asp:Button ID="Button4" OnClick="AddNewUser" Text="Add User" runat="server" />
            <br />
            <asp:Label ID="addUserDebug" runat="server"></asp:Label>
        </td></tr>
        <!--
             Middle-left, displays information of the selected user.
         -->
        <tr><td align="right">
            ID:
            <asp:TextBox ID="userIDBox" Enabled="false" Width="150" runat="server"></asp:TextBox>
            <br />
            Password:
            <asp:TextBox ID="userPasswordBox" Width="150" runat="server"></asp:TextBox>
            <br />
            <asp:RadioButtonList ID="userAdminChoice" runat="server">
                <asp:ListItem Text="Admin"></asp:ListItem>
                <asp:ListItem Text="Not Admin"></asp:ListItem>
            </asp:RadioButtonList>
            <asp:Label ID="userDebug" runat="server"></asp:Label>
        </td>
        <td>
            <asp:Button ID="Button5" OnClick="UpdateUserInfo" Text="Update User Info" runat="server" />
        </td></tr>
        <!--
             Bottom-left, displays and adds app permissions for the selected user.
         -->
        <tr><td align="right">
            Permissions:
            <asp:ListBox Width="230" Height="100" ID="userAppsBox" SelectionMode="Single" runat="server"></asp:ListBox>
        </td>
        <td>
            <asp:Button ID="Button6" OnClick="DeleteAppPermission" Text="Delete App Permission" runat="server" />
        </td>
        <td align="right" valign="middle" rowspan="2">
            Name:
            <asp:TextBox ID="appNameBox" runat="server"></asp:TextBox>
            <br />
            Version:
            <asp:TextBox ID="appVersionBox" runat="server"></asp:TextBox>
            <br />
            <b>ID:</b>
            <asp:TextBox ID="appIDBox" runat="server"></asp:TextBox>
            <br />
            <asp:Button ID="Button7" OnClick="AddEditApplication" Text="Add/Edit Application" runat="server" />
            <br />
            <asp:Button ID="Button8" OnClick="RemoveApplication" Text="Remove Application" runat="server" />
            <br />
            <asp:Label ID="AppDebug" runat="server"></asp:Label>
        </td>
        </tr>
        <tr><td align="right">
            Applications:
            <asp:ListBox Width="230" Height="100" ID="appListBox" AutoPostBack="true" OnSelectedIndexChanged="DisplayAppData" SelectionMode="Single" runat="server"></asp:ListBox>
        </td>
        <td>
            <asp:Button ID="Button9" OnClick="AddAppPermission" Text="Add App Permission" runat="server" />
        </td></tr></table>
        <asp:Label ID="updateDebug" runat="server" />
    </div>
    </form>
</body>
</html>
