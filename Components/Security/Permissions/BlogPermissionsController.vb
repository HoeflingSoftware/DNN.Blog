Imports System
Imports System.Data
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Entities.Users
Imports DotNetNuke.Modules.Blog.Common.Constants
Imports DotNetNuke.Modules.Blog.Entities.Blogs

Namespace Security.Permissions
 Partial Public Class BlogPermissionsController

#Region " Public Methods "
  Public Shared Function HasBlogPermission(objBlogPermissions As BlogPermissionCollection, PermissionId As Integer) As Boolean
   If BlogUser.GetCurrentUser.IsAdministrator Then Return True
   Dim m As BlogPermissionCollection = objBlogPermissions
   Dim i As Integer
   For i = 0 To m.Count - 1
    Dim mp As BlogPermissionInfo
    mp = m(i)
    If mp.PermissionId = PermissionId AndAlso (DotNetNuke.Security.PortalSecurity.IsInRoles(mp.RoleName) Or (DotNetNuke.Entities.Users.UserController.GetCurrentUserInfo.Username = mp.Username And mp.UserId <> glbUserNothing)) AndAlso (mp.Expires > Now Or mp.Expires = Date.MinValue) Then
     Return True
    End If
   Next
   Return False
  End Function

  Public Shared Function HasBlogPermission(objBlogPermissions As BlogPermissionCollection, PermissionKey As String) As Boolean
   If BlogUser.GetCurrentUser.IsAdministrator Then Return True
   Dim m As BlogPermissionCollection = objBlogPermissions
   Dim i As Integer
   For i = 0 To m.Count - 1
    Dim mp As BlogPermissionInfo
    mp = m(i)
    If mp.PermissionKey = PermissionKey AndAlso (DotNetNuke.Security.PortalSecurity.IsInRoles(mp.RoleName) Or (DotNetNuke.Entities.Users.UserController.GetCurrentUserInfo.Username = mp.Username And mp.UserId <> glbUserNothing)) AndAlso (mp.Expires > Now Or mp.Expires = Date.MinValue) Then
     Return True
    End If
   Next
   Return False
  End Function

  Public Shared Function HasBlogPermission(objBlogPermissions As BlogPermissionCollection, PermissionKey As String, User As UserInfo) As Boolean
   If BlogUser.GetUser(User).IsAdministrator Then Return True
   Dim m As BlogPermissionCollection = objBlogPermissions
   Dim i As Integer
   For i = 0 To m.Count - 1
    Dim mp As BlogPermissionInfo
    mp = m(i)
    If mp.PermissionKey = PermissionKey AndAlso (User.IsInRole(mp.RoleName) Or (User.Username = mp.Username And mp.UserId <> glbUserNothing)) AndAlso (mp.Expires > Now Or mp.Expires = Date.MinValue) Then
     Return True
    End If
   Next
   Return False
  End Function

  Public Shared Function GetBlogPermissionsCollection(BlogId As Integer) As BlogPermissionCollection

   Dim epc As New BlogPermissionCollection
   If BlogId > 0 Then
    Using ir As IDataReader = Data.DataProvider.Instance().GetBlogPermissionsByBlog(BlogId)
     Do While ir.Read
      epc.Add(FillBlogPermissionInfo(ir, False, False))
     Loop
    End Using
   End If
   Return epc

  End Function

  Public Shared Sub DeleteBlogPermissionsByBlogId(BlogId As Integer)

   Data.DataProvider.Instance().DeleteBlogPermissions(BlogId)

  End Sub

  Public Shared Sub UpdateBlogPermissions(Blog As BlogInfo)
   DeleteBlogPermissionsByBlogId(Blog.BlogID)
   For Each objBlogPermission As BlogPermissionInfo In Blog.Permissions
    objBlogPermission.BlogId = Blog.BlogID
    If objBlogPermission.AllowAccess Then
     Try
      BlogPermissionsController.AddBlogPermission(objBlogPermission)
     Catch
     End Try
    End If
   Next
  End Sub

  Public Shared Function GetUsersByBlogPermission(portalId As Integer, blogId As Integer, permissionId As Integer) As Dictionary(Of String, UserInfo)
   Return DotNetNuke.Common.Utilities.CBO.FillDictionary(Of String, UserInfo)("username", Data.DataProvider.Instance.GetUsersByBlogPermission(portalId, blogId, permissionId))
  End Function
  Public Shared Function GetUsersByBlogPermission(portalId As Integer, blogId As Integer, permission As String) As Dictionary(Of String, UserInfo)
   Return DotNetNuke.Common.Utilities.CBO.FillDictionary(Of String, UserInfo)("username", Data.DataProvider.Instance.GetUsersByBlogPermission(portalId, blogId, GetPermissionId(permission)))
  End Function

  Public Shared Function GetPermissionId(permission As String) As Integer
   Select Case permission.ToUpper
    Case "ADD"
     Return BlogPermissionTypes.ADD
    Case "ADDCOMMENT"
     Return BlogPermissionTypes.ADDCOMMENT
    Case "APPROVE"
     Return BlogPermissionTypes.APPROVE
    Case "APPROVECOMMENT"
     Return BlogPermissionTypes.APPROVECOMMENT
    Case "EDIT"
     Return BlogPermissionTypes.EDIT
    Case Else
     Return -1
   End Select
  End Function
#End Region

#Region " Fill Methods "
  Private Shared Function FillBlogPermissionInfo(dr As IDataReader) As BlogPermissionInfo
   Return FillBlogPermissionInfo(dr, True, True)
  End Function

  Private Shared Function FillBlogPermissionInfo(dr As IDataReader, CheckForOpenDataReader As Boolean, CloseReader As Boolean) As BlogPermissionInfo

   Dim objBlogPermission As New BlogPermissionInfo
   Dim canContinue As Boolean = True
   If CheckForOpenDataReader Then
    canContinue = False
    If dr.Read Then
     canContinue = True
    End If
   End If
   If canContinue Then
    With objBlogPermission
     .AllowAccess = Convert.ToBoolean(Null.SetNull(dr("AllowAccess"), objBlogPermission.AllowAccess))
     .DisplayName = Convert.ToString(Null.SetNull(dr("DisplayName"), objBlogPermission.DisplayName))
     .BlogId = Convert.ToInt32(Null.SetNull(dr("BlogId"), objBlogPermission.BlogId))
     .Expires = Convert.ToDateTime(Null.SetNull(dr("Expires"), objBlogPermission.Expires))
     .PermissionId = Convert.ToInt32(Null.SetNull(dr("PermissionId"), objBlogPermission.PermissionId))
     Select Case .PermissionId
      Case BlogPermissionTypes.ADD
       .PermissionKey = "ADD"
      Case BlogPermissionTypes.EDIT
       .PermissionKey = "EDIT"
      Case BlogPermissionTypes.APPROVE
       .PermissionKey = "APPROVE"
      Case BlogPermissionTypes.ADDCOMMENT
       .PermissionKey = "ADDCOMMENT"
      Case BlogPermissionTypes.APPROVECOMMENT
       .PermissionKey = "APPROVECOMMENT"
     End Select
     .RoleId = Convert.ToInt32(Null.SetNull(dr("RoleID"), objBlogPermission.RoleId))
     If .RoleId > -1 Then
      Dim rn As String = Convert.ToString(Null.SetNull(dr("RoleName"), ""))
      .RoleName = rn
     End If
     .UserId = Convert.ToInt32(Null.SetNull(dr("UserID"), objBlogPermission.UserId))
     .Username = Convert.ToString(Null.SetNull(dr("UserName"), objBlogPermission.Username))
    End With
   Else
    objBlogPermission = Nothing
   End If

   If CloseReader Then
    dr.Close()
   End If

   Return objBlogPermission

  End Function

#End Region

 End Class



End Namespace
