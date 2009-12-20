'
' DotNetNuke -  http://www.dotnetnuke.com
' Copyright (c) 2002-2005
' by Perpetual Motion Interactive Systems Inc. ( http://www.perpetualmotion.ca )
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'-------------------------------------------------------------------------

Imports System
Imports System.Web.UI
Imports DotNetNuke.Modules.Blog.Business
Imports DotNetNuke.Security
Imports DotNetNuke.UI.WebControls
Imports DotNetNuke
Imports DotNetNuke.Common.Globals
Imports System.Globalization
Imports DotNetNuke.Services.Localization
Imports DotNetNuke.Services.Exceptions
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Entities.Users



Partial Class ViewEntry
 Inherits BlogModuleBase

#Region "Private members"
 Private m_oBlogController As New BlogController
 Private m_oBlog As BlogInfo
 Private m_oParentBlog As BlogInfo
 Private m_oEntryController As New EntryController
 Private m_oEntry As EntryInfo
 Private m_oEntryID As Integer = -1
#End Region

#Region "Controls"
 'Protected WithEvents lblAddComment As System.Web.UI.WebControls.Label
 Protected WithEvents imgGravatar As System.Web.UI.WebControls.Image
 Protected WithEvents lnkCheckGravatar As System.Web.UI.WebControls.LinkButton
 'Protected WithEvents lnkPermaLink As System.Web.UI.WebControls.HyperLink
 Protected WithEvents AuthorRow As System.Web.UI.HtmlControls.HtmlTableRow
 Protected WithEvents cbUseGravatar As System.Web.UI.WebControls.CheckBox


#End Region

#Region "Event Handlers"
 Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
  Try
   If Not Page.IsPostBack Then
    If m_oEntry Is Nothing Then
     Response.Redirect(NavigateURL(), True)
    End If

    If Not m_oBlog Is Nothing Then

     If Not Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, ModuleId) Then
      If UserId = -1 Then
       If m_oBlog.MustApproveAnonymous Then
        DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(cmdAddComment, Localization.GetString("cmdAddCommentMessage", LocalResourceFile))
       End If
      Else
       If m_oBlog.MustApproveComments Then
        DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(cmdAddComment, Localization.GetString("cmdAddCommentMessage", LocalResourceFile))
       End If
      End If
     End If

     If m_oBlog.ShowFullName Then
      lblUserID.Text = m_oBlog.UserFullName
     Else
      lblUserID.Text = m_oBlog.UserName
     End If
     'lblUserID.NavigateUrl = NavigateURL(TabId, "ViewProfile", "UserTicket=" & u.EncryptParameter(m_oBlog.UserID.ToString))

     Me.BasePage.Author = m_oBlog.UserFullName
     pnlComments.Visible = m_oBlog.AllowComments

     'DR-04/20/2009-BLG-6908
     If Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, Me.ModuleId) Then
      DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(btDeleteAllUnapproved, Localization.GetString("msgDeleteAllUnapproved", LocalResourceFile))
      DotNetNuke.UI.Utilities.ClientAPI.AddButtonConfirm(lnkDeleteAllUnapproved, Localization.GetString("msgDeleteAllUnapproved", LocalResourceFile))
      btDeleteAllUnapproved.Visible = True
      lnkDeleteAllUnapproved.Visible = True
     Else
      btDeleteAllUnapproved.Visible = False
      lnkDeleteAllUnapproved.Visible = False
     End If

     lnkBlogs.NavigateUrl = NavigateURL()
     If m_oBlog.ParentBlogID > -1 Then
      m_oParentBlog = m_oBlogController.GetBlog(m_oBlog.ParentBlogID)
      lnkParentBlog.Text = m_oParentBlog.Title
      lnkParentBlog.NavigateUrl = NavigateURL(Me.TabId, "", "BlogID=" & m_oParentBlog.BlogID.ToString())
      lnkChildBlog.Visible = True
      lnkChildBlog.Text = m_oBlog.Title
      lnkChildBlog.NavigateUrl = NavigateURL(Me.TabId, "", "BlogID=" & m_oBlog.BlogID.ToString())
      imgParentChildSeparator.Visible = True
     Else
      lnkParentBlog.Text = m_oBlog.Title
      lnkParentBlog.NavigateUrl = NavigateURL(Me.TabId, "", "BlogID=" & m_oBlog.BlogID.ToString())
      lnkChildBlog.Visible = False
      imgParentChildSeparator.Visible = False
     End If
     If m_oEntry.DisplayCopyright Then
      If m_oEntry.Copyright.Length > 0 Then
       Me.BasePage.Copyright = m_oEntry.Copyright
       lblCopyright.Text = m_oEntry.Copyright
       lblCopyright.Visible = True
      End If
     End If
    End If
    If Not m_oEntry Is Nothing Then

     'DW - 08/14/2008 - Added code to issue a 301 Redirect in cases where the URL of the page is not the same
     ' as that created by the new BlogNavigateURL function
     Dim requestedUrl As String = DirectCast(HttpContext.Current.Items()("UrlRewrite:OriginalUrl"), String)
     ' DW - 11/12/2008 - Replaced with Permalink
     Dim correctUrl As String = m_oEntry.PermaLink
     If (BlogSettings.ShowSeoFriendlyUrl And Not requestedUrl Is Nothing And _
         (Not requestedUrl.ToLower().EndsWith(correctUrl.ToLower()) _
         And Not System.Web.HttpUtility.UrlDecode(requestedUrl.ToLower()).EndsWith(correctUrl.ToLower()))) Then
      'NOTE: We use EndsWith here because NavigateURL returns a relative URL to BlogNavigateURL
      '       when friendly URLs is not turned on for the portal.
      '301 Redirect to the correct format for the page
      Response.Status = "301 Moved Permanently"
      Response.AddHeader("Location", correctUrl)
      Response.AddHeader("X-Blog-Redirect-Reason", _
          "No match for requested Url (" & requestedUrl & ").  Correct url is " & correctUrl)
      Response.End()
     End If

     lblBlogTitle.Text = m_oEntry.Title
     'lblBlogTitle.NavigateUrl = m_oEntry.PermaLink

     lblTrackback.Text = Utility.GetTrackbackRDF(NavigateURL(), m_oEntry)
     lblDateTime.Text = Utility.FormatDate(m_oEntry.AddedDate, m_oBlog.Culture, m_oBlog.DateFormat, m_oBlog.TimeZone)
     lblEntryMonth.Text = GetMonth(m_oEntry.AddedDate, m_oBlog.TimeZone)
     lblEntryDay.Text = GetDay(m_oEntry.AddedDate, m_oBlog.TimeZone)

     'Antonio Chagoury - 4/11/2008
     If BlogSettings.ShowSocialBookmarks Then
      AddSocialBookmarks(m_oEntry.Title, m_oEntry.PermaLink)
     End If

     'Antonio Chagoury
     'Leave the module title as it should be
     'Added a label to display the Blog Entry Title
     'Don Worthley - 04/05/2008
     'Added back in

     If BlogSettings.ShowUniqueTitle Then
      Try             ' 4.0.1 Bug
       Me.BasePage.Title = Me.BasePage.Title & " - " & m_oEntry.Title
      Catch
      End Try
     End If

     'Rip Rowan 7/5/2008
     'Put description on page, show / hide based on setting
     lblSummary.Text = Server.HtmlDecode(m_oEntry.Description)

     'lblSummary.CssClass = "blog_entry_description"
     If BlogSettings.ShowSummary Then
      lblSummary.Visible = True
     Else
      lblSummary.Visible = False
     End If

     lblEntry.Text = Server.HtmlDecode(m_oEntry.Entry)

     pnlComments.Visible = (m_oBlog.AllowComments Or m_oEntry.AllowComments)

     'DW - 06/17/2008
     ' Changed to show RSS feed for blog rather than page.  Was rssentryid= & m_oEntry.EntryID.ToSring
     ' Also changed Visible code to only show Rss link based on whether the blog is syndicated.
     lnkRss.Visible = True
     ' DW - 06/25/2008 - Updated to show blog rss only if sydicated independently.  If not, then
     ' we show the parent blog if the parent blog is syndicated.
     If m_oBlog.SyndicateIndependant Or m_oBlog.ParentBlogID = -1 Then
      ' Child blog with SyndicateIndependently or Parent Blog
      lnkRss.NavigateUrl = NavigateURL(Me.TabId, "", "rssid=" & m_oEntry.BlogID.ToString)
     ElseIf m_oBlog.Syndicated And m_oBlog.ParentBlogID <> -1 Then
      ' Child blog that is Syndicated, but not indpenedently - Show parent blog RSS feed
      lnkRss.NavigateUrl = NavigateURL(Me.TabId, "", "rssid=" & m_oBlog.ParentBlogID.ToString())
     Else
      ' If none of the above, then don't show the RSS link.
      lnkRss.Visible = False
     End If


     ' 07/12/2008 Rip Rowan
     ' Cleaned up logic in next 7 lines
     ' And implemented fix for Name display / edit bug
     If pnlComments.Visible Then
      BindCommentsList()
     End If

     pnlAddComment.Visible = (m_oBlog.AllowComments Or m_oEntry.AllowComments) And (m_oBlog.AllowAnonymous Or Me.UserId > -1)
     pnlLogin.Visible = (m_oBlog.AllowComments Or m_oEntry.AllowComments) And Not (m_oBlog.AllowAnonymous Or Me.UserId > -1)
     txtAuthor.ReadOnly = Not m_oBlog.AllowAnonymous
     txtEmail.ReadOnly = Not m_oBlog.AllowAnonymous
     txtWebsite.ReadOnly = Not m_oBlog.AllowAnonymous

     'lnkPermaLink.NavigateUrl = m_oEntry.PermaLink

     'Antonio Chagoury
     'BLG(-4471): Fixed CDATA enconding in 
     'title and description for trackbacks
     lnkTrackBack.NavigateUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) & Me.ModulePath & "Trackback.aspx?id=" & m_oEntry.EntryID & "&blogid=" & m_oEntry.BlogID

     'Only show the add email row if we are showing Gravatars - DW - 4/22/2008
     trGravatarEmail.Visible = BlogSettings.ShowGravatars

     'Only show the add website row if we are showing URL with comments - 
     'DW(-6 / 6 / 2008)
     trCommentWebsite.Visible = BlogSettings.ShowWebsite

     ' Only show the title row if unique comment titles are allowed in the settings
     ' Rip Rowan 6/13/2008
     trCommentTitle.Visible = BlogSettings.ShowCommentTitle

     'Additionally, only show Gravatar Preview and checkbox if showing Gravatars
     'DW - 06/06/2008
     'Added trGravatarEmail
     'RR - 06/12/2008

     trGravatarEmail.Visible = BlogSettings.ShowGravatars
     trUseGravatar.Visible = BlogSettings.ShowGravatars
     tdGravatarPreview.Visible = BlogSettings.ShowGravatars
     If BlogSettings.ShowGravatars Then
      tdAuthor.ColSpan = 1
     Else
      tdAuthor.ColSpan = 2
     End If

    End If

    rowCaptcha.Visible = (pnlComments.Visible And m_oBlog.UseCaptcha And Not Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, ModuleId))
   End If

   AddGravatarImagePreview()

   txtClientIP.Text = HttpContext.Current.Request.UserHostAddress.ToString

   If rowCaptcha.Visible Then
    ctlCaptcha.ErrorMessage = Localization.GetString("InvalidCaptcha", Me.LocalResourceFile)
    ctlCaptcha.Text = Localization.GetString("CaptchaText", Me.LocalResourceFile)
   End If


   Dim tc As New TagController
   Dim taglist As ArrayList = tc.ListTagsByEntry(m_oEntry.EntryID)
   rptTags.DataSource = taglist
   rptTags.DataBind()

   rptCategories.DataSource = (New CategoryController).ListCatsByEntry(m_oEntry.EntryID)
   rptCategories.DataBind()

  Catch exc As Exception
   ProcessModuleLoadException(Me, exc)
  End Try
 End Sub

 'Private Sub rptTags_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.RepeaterItemEventArgs) Handles rptTags.ItemDataBound

 ' If (e.Item.ItemType = ListItemType.Item Or e.Item.ItemType = ListItemType.AlternatingItem) Then

 '  Dim lnkTag As HyperLink = CType(e.Item.FindControl("lnkTags"), HyperLink)

 '  'Dim eid As Integer = CType(CType(e.Item.DataItem, TagInfo).EntryID, Integer)

 '  Dim ti As String = (New TagController).GetTagsByEntry(m_oEntryID)
 '  Dim litTags As Literal = CType(e.Item.FindControl("litTags"), Literal)
 '  Dim tbTags As TextBox = CType(e.Item.FindControl("tbTags"), TextBox)
 '  litTags.Text = ti
 '  tbTags.Text = ti
 '  tbTags.Visible = False

 '  Dim cc As New CategoryController
 '  Dim ci As List(Of Business.CategoryInfo) = cc.ListCatsByEntry(m_oEntryID)
 '  Dim cl As List(Of Business.CategoryInfo) = cc.ListCategoriesSorted(PortalId)

 '  Dim dlCat As DropDownList = CType(e.Item.FindControl("ddlCat"), DropDownList)

 '  dlCat.DataSource = cl

 '  dlCat.DataBind()
 '  dlCat.Items.Insert(0, New ListItem(" - Uncategorized - ", "-1"))

 '  Dim litCat As Literal = CType(e.Item.FindControl("litCat"), Literal)
 '  If ci.Count > 0 Then
 '   litCat.Text = ci(0).Category
 '   If Not dlCat.Items.FindByValue(ci(0).CatID.ToString) Is Nothing Then
 '    dlCat.Items.FindByValue(ci(0).CatID.ToString).Selected = True
 '   End If
 '  End If
 '  dlCat.Visible = False

 '  Dim litTitle As Literal = CType(e.Item.FindControl("litTitle"), Literal)
 '  If litTitle.Text.Length > 30 Then litTitle.Text = Left(litTitle.Text, 30) + "..."
 '  Dim tbTitle As TextBox = CType(e.Item.FindControl("tbTitle"), TextBox)
 '  tbTitle.Visible = False

 '  Dim cbPub As CheckBox = CType(e.Item.FindControl("cbPublished"), CheckBox)
 '  cbPub.Enabled = False
 '  Dim cbComments As CheckBox = CType(e.Item.FindControl("cbComments"), CheckBox)
 '  cbComments.Enabled = False

 ' End If

 'End Sub

 Private Sub lnkAddComment_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
  Response.Redirect(EditUrl("EntryID", m_oEntry.EntryID.ToString(), "EditComment"), True)
 End Sub

 Private Sub lstComments_ItemDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.DataListItemEventArgs) Handles lstComments.ItemDataBound
  Dim lnkEditComment As System.Web.UI.WebControls.ImageButton = CType(e.Item.FindControl("lnkEditComment"), System.Web.UI.WebControls.ImageButton)
  Dim lnkApproveComment As System.Web.UI.WebControls.ImageButton = CType(e.Item.FindControl("lnkApproveComment"), System.Web.UI.WebControls.ImageButton)
  Dim btEditComment As System.Web.UI.WebControls.LinkButton = CType(e.Item.FindControl("btEditComment"), System.Web.UI.WebControls.LinkButton)
  Dim btApproveComment As System.Web.UI.WebControls.LinkButton = CType(e.Item.FindControl("btApproveComment"), System.Web.UI.WebControls.LinkButton)
  Dim lblTitle As System.Web.UI.WebControls.Label = CType(e.Item.FindControl("lblTitle"), System.Web.UI.WebControls.Label)
  Dim lnkUserName As System.Web.UI.WebControls.HyperLink = CType(e.Item.FindControl("lnkUserName"), System.Web.UI.WebControls.HyperLink)
  Dim lblCommentDate As System.Web.UI.WebControls.Label = CType(e.Item.FindControl("lblCommentDate"), System.Web.UI.WebControls.Label)
  Dim imgGravatar As System.Web.UI.WebControls.Image = CType(e.Item.FindControl("imgGravatar"), System.Web.UI.WebControls.Image)
  Dim divBlogBubble As System.Web.UI.WebControls.Panel = CType(e.Item.FindControl("divBlogBubble"), System.Web.UI.WebControls.Panel)
  Dim divBlogGravatar As System.Web.UI.WebControls.Panel = CType(e.Item.FindControl("divBlogGravatar"), System.Web.UI.WebControls.Panel)
  Dim lnkDeleteComment As System.Web.UI.WebControls.ImageButton = CType(e.Item.FindControl("lnkDeleteComment"), System.Web.UI.WebControls.ImageButton)
  Dim btDeleteComment As System.Web.UI.WebControls.LinkButton = CType(e.Item.FindControl("btDeleteComment"), System.Web.UI.WebControls.LinkButton)

  Dim commentInfo As CommentInfo = CType(e.Item.DataItem, CommentInfo)

  If m_oBlog.UserID = Me.UserId Then
   lnkEditComment.Visible = Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, Me.ModuleId)
  Else
   lnkEditComment.Visible = Utility.HasBlogPermission(Me.UserId, commentInfo.UserID, Me.ModuleId)
  End If
  lnkDeleteComment.Visible = lnkEditComment.Visible
  btDeleteComment.Visible = lnkEditComment.Visible

  'DW - 06/06/2008
  'Set a unique CSS class for the blog bubbles of the blog owner
  If m_oBlog.UserID = commentInfo.UserID Then
   divBlogBubble.CssClass = "blog_bubble_owner"
  End If


  btEditComment.Visible = lnkEditComment.Visible
  ' To Maintain compatibility with previous versions
  If commentInfo.Title Is Nothing Then
   lblTitle.Text = Localization.GetString("msgRe", LocalResourceFile) & m_oEntry.Title
  Else
   If commentInfo.Title = "" Then
    lblTitle.Text = Localization.GetString("msgRe", LocalResourceFile) & m_oEntry.Title
   Else
    lblTitle.Text = commentInfo.Title
   End If
  End If

  ' Rip Rowan 6/13/2008
  ' Hide comment titles if not enabled in settings
  lblTitle.Visible = BlogSettings.ShowCommentTitle

  lblCommentDate.Text = Utility.FormatDate(commentInfo.AddedDate, m_oBlog.Culture, m_oBlog.DateFormat, m_oBlog.TimeZone)
  If Not commentInfo.Approved Then
   lnkApproveComment.Visible = True
  Else
   lnkApproveComment.Visible = False
  End If
  btApproveComment.Visible = lnkApproveComment.Visible
  Dim tmpName As String

  ' Rip Rowan 7/12/2008
  ' if no author, then try to display username (will return "anonymous" or equiv if no username)

  If commentInfo.Author <> "" Then
   tmpName = commentInfo.Author
  Else
   tmpName = commentInfo.UserName
  End If

  ' Make into a hyperlink if a website was entered.
  If commentInfo.Website <> String.Empty AndAlso BlogSettings.ShowWebsite Then
   tmpName = String.Format(Localization.GetString("lblFormatUserNameUrl.Text", LocalResourceFile), commentInfo.Website, tmpName)
  End If

  lnkUserName.Text = String.Format(Localization.GetString("lblFormatUserName.Text", LocalResourceFile), tmpName)

  ' Rip Rowan - 6/14/2008
  ' Suppress image completely if no email
  ' prevents lots of spurious (and wrong) avatars from pre-3.4.1 comments
  ' going forward, the email field automatically populates with the IP address if left blank

  If BlogSettings.ShowGravatars And commentInfo.Email.Length > 5 Then
   imgGravatar.ImageUrl = GetGravatarUrl(commentInfo.Email)
   imgGravatar.Width = New Unit(BlogSettings.GravatarImageWidth)
   imgGravatarPreview.Width = New Unit(BlogSettings.GravatarImageWidth)
   'imgGravatar.Visible = True
   divBlogGravatar.Visible = True
  Else
   'imgGravatar.Visible = False
   divBlogGravatar.Visible = False

  End If

 End Sub

 Private Sub lstComments_ItemCommand(ByVal source As Object, ByVal e As System.Web.UI.WebControls.DataListCommandEventArgs) Handles lstComments.ItemCommand
  Select Case e.CommandName
   Case "EditComment"
    valCommentAuthor.Enabled = False
    valCommentTitle.Enabled = False
    valComment.Enabled = False
    valSummary.Enabled = False

    Dim oComment As CommentInfo = New CommentController().GetComment(Int32.Parse(CType(e.CommandArgument, String)))
    If Not oComment Is Nothing Then
     txtCommentTitle.Text = oComment.Title
     txtEmail.Text = oComment.Email
     txtWebsite.Text = oComment.Website
     txtComment.Text = Utility.removeAllHtmltags(Server.HtmlDecode(oComment.Comment))
     txtAuthor.Text = oComment.Author
     ViewState("CommentID") = oComment.CommentID

     cmdAddComment.Text = Localization.GetString("msgUpdateComment", LocalResourceFile)
     cmdDeleteComment.Visible = True
    End If
   Case "ApproveComment"
    Dim oComment As CommentInfo = New CommentController().GetComment(Int32.Parse(CType(e.CommandArgument, String)))
    oComment.Approved = True
    Dim objCtlComment As New CommentController
    objCtlComment.UpdateComment(oComment)
    BindCommentsList()
   Case "DeleteComment"
    ' DR 01/21/2008 - Fix BLG-8849
    ' Added fast comment deletion.
    Dim oCommentController As New CommentController
    oCommentController.DeleteComment(Int32.Parse(CType(e.CommandArgument, String)))
    BindCommentsList()
  End Select
 End Sub

 Private Sub cmdAddComment_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdAddComment.Click
  Try
   valCommentAuthor.Enabled = True
   valCommentTitle.Enabled = True
   valComment.Enabled = True
   valSummary.Enabled = True
   Page.Validate()
   If Page.IsValid And ((m_oBlog.UseCaptcha And ctlCaptcha.IsValid) Or (Not m_oBlog.UseCaptcha) Or Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, ModuleId)) Then
    Dim oComment As New CommentInfo
    Dim oCommentController As New CommentController

    If Not ViewState("CommentID") Is Nothing Then
     oComment = oCommentController.GetComment(Int32.Parse(CType(ViewState("CommentID"), String)))
     ViewState("CommentID") = Nothing
    Else
     oComment = CType(CBO.InitializeObject(oComment, GetType(CommentInfo)), CommentInfo)
     oComment.EntryID = m_oEntry.EntryID
     oComment.UserID = Me.UserId
    End If
    Dim objSec As New PortalSecurity
    oComment.Title = objSec.InputFilter(txtCommentTitle.Text, PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting)
    'DW - 06/06/2008 - Added code to use Regex to add rel="nofollow" to any hyperlinks 
    ' added in the comment.  This check is done in CleanLinksInHtml
    oComment.Comment = objSec.InputFilter(Utility.FormatHTML(Utility.CleanCommentHtml(txtComment.Text, BlogSettings.AllowCommentAnchors, BlogSettings.AllowCommentImages, BlogSettings.AllowCommentFormatting)), PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting)
    oComment.Author = objSec.InputFilter(txtAuthor.Text, PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting)

    If txtEmail.Text <> String.Empty Then
     oComment.Email = objSec.InputFilter(txtEmail.Text, PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting)
    Else
     oComment.Email = objSec.InputFilter(HttpContext.Current.Request.UserHostAddress.ToString, PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting)
    End If

    oComment.Website = Utility.EnsureBeginningHttp(objSec.InputFilter(txtWebsite.Text, PortalSecurity.FilterFlag.NoMarkup Or PortalSecurity.FilterFlag.NoScripting))

    If oComment.UserID = -1 Then
     If m_oBlog.MustApproveAnonymous Then
      oComment.Approved = False
     Else
      oComment.Approved = True
     End If
    Else
     ' Rip Rowan 7/08/2008
     ' next three lines make no sense, removing
     ' should only ever save whatever is in the name field
     ' shouldn't ever change the value from what the user belives will be entered!!!
     'If oComment.CommentID = -1 Then     ' for new comments save username to author field
     '    oComment.Author = Me.UserInfo.Username
     'End If
     If m_oBlog.MustApproveComments Then
      oComment.Approved = False
     Else
      oComment.Approved = True
     End If
    End If
    If Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, ModuleId) Then
     oComment.Approved = True
    End If
    If oComment.CommentID > -1 Then
     oCommentController.UpdateComment(oComment)
    Else
     oComment.CommentID = oCommentController.AddComment(oComment)
     If m_oBlog.EmailNotification = True Then
      sendMail(m_oBlog, oComment)
     End If
    End If
    BindCommentsList()
   End If
  Catch exc As Exception

  End Try
 End Sub

 Private Sub cmdDeleteComment_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdDeleteComment.Click
  If Not ViewState("CommentID") Is Nothing Then
   Dim oCommentController As New CommentController
   oCommentController.DeleteComment(Int32.Parse(CType(ViewState("CommentID"), String)))
   ViewState("CommentID") = Nothing
   BindCommentsList()
  End If
 End Sub

 Private Sub cmdLogin_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdLogin.Click
  Dim LoginURL As String
  LoginURL = m_oEntry.PermaLink + "?ctl=login"
  Response.Redirect(LoginURL, True)
 End Sub

 Private Sub cmdCancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCancel.Click
  Try
   Response.Redirect(NavigateURL(), True)
  Catch exc As Exception    'Module failed to load
   ProcessModuleLoadException(Me, exc)
  End Try
 End Sub

 Private Sub cmdPrint_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles cmdPrint.Click
  Response.Clear()
  Dim strPrintPage As String
  Dim strAuthor As String

  ' Rip Rowan 2008.05.29
  ' Set author based on username / fullname section
  ' Fixed BLG-6964

  If m_oBlog.ShowFullName Then
   strAuthor = m_oBlog.UserFullName
  Else
   strAuthor = m_oBlog.UserName
  End If

  ' Rip Rowan 2008.05.29
  ' Changed title to entry.title & (Print Friendly View)
  ' Improves output when printed
  strPrintPage = "<html><head><title>" & m_oEntry.Title & " (Print Friendly View)</title></head><body bgcolor=""white"">" & _
  "<table width=""100%""><tr><td align=""left"">" & _
  "<h1>" & m_oEntry.Title & "</h1></td>" & _
  "<td align=""right""><h5>" & Localization.GetString("lblPostedBy.Text", LocalResourceFile) & strAuthor & " " & m_oEntry.AddedDate.ToLocalTime().ToString() & "</h5></td></tr></table>" & _
  "<hr />"

  If m_oEntry.Description <> String.Empty Then
   ' Rip Rowan 2008.05.29
   ' HTMLDecode the entry.description
   ' Fixed  BLG-7641
   strPrintPage += Server.HtmlDecode(m_oEntry.Description) & Environment.NewLine & "<hr />"
  End If

  strPrintPage += Server.HtmlDecode(m_oEntry.Entry) & Environment.NewLine & _
  "<hr />" & _
  Server.HtmlDecode(m_oEntry.Copyright) & Environment.NewLine & _
  "</body></html>"

  Response.Write(strPrintPage)
  Response.End()

 End Sub

 Private Sub lnkDeleteAllUnapproved_Click(ByVal sender As Object, ByVal e As System.Web.UI.ImageClickEventArgs) Handles lnkDeleteAllUnapproved.Click
  If Not m_oEntry Is Nothing Then
   Dim oCommentController As New CommentController
   oCommentController.DeleteAllUnapproved(m_oEntry.EntryID)
   BindCommentsList()
  End If
 End Sub

 Private Sub btDeleteAllUnapproved_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btDeleteAllUnapproved.Click
  If Not m_oEntry Is Nothing Then
   Dim oCommentController As New CommentController
   oCommentController.DeleteAllUnapproved(m_oEntry.EntryID)
   BindCommentsList()
  End If
 End Sub

#End Region

#Region "Private Methods"

 Private Sub AddGravatarImagePreview()

  ' Rip Rowan -- 5/29/2008
  ' Set up MD5 script block for gravatar image preview swapping

  If Not Page.ClientScript.IsClientScriptBlockRegistered("GR_MD5") Then
   Dim GrMD5Script As String = "<script src=""" & ModulePath & "js/MD5.js"" type=""text/javascript""></script>"
   Page.ClientScript.RegisterClientScriptBlock(Me.GetType(), "SB_MD5", GrMD5Script)
  End If


  ' Rip Rowan -- 6/12/2008
  ' Set up image swap javascript

  Dim strOnchangeEmail As String = "'" + GravatarURLPrefix() + "' + hex_md5(" + txtEmail.ClientID + ".value) + '" + GravatarURLSuffix() + "'"
  Dim strOnchangeNoEmail As String = "'" + GravatarURLPrefix() + "' + hex_md5(" + txtClientIP.ClientID + ".value) + '" + GravatarURLSuffix() + "'"

  Dim strDefault As String = "'" + GravatarURLPrefix() + "b6267c6fe44b457964aed2218e3ef8fe" + GravatarURLSuffix() + "'"

  txtEmail.Attributes.Add("onchange", "javascript:if(" + txtEmail.ClientID + ".value.length > 5) {" + imgGravatarPreview.ClientID + ".src=" + strOnchangeEmail _
                           + ";}else{" + imgGravatarPreview.ClientID + ".src=" + strOnchangeNoEmail + ";}")

  imgGravatarPreview.ImageUrl = GetGravatarUrl(txtEmail.Text)


 End Sub

 Private Sub AddSocialBookmarks(ByVal EntryTitle As String, ByVal EntryUrl As String)
  'Antonio Chagoury - 5/11/2008
  'Adding social bookmarks

  'Initialize the ShareBadge Chicklets Array
  If Not Page.ClientScript.IsClientScriptBlockRegistered("SB_CHICKLETS") Then
   Dim SbChickletsScript As String = "<script language=""javascript1.2"" type=""text/javascript"">" _
   & "var strImagePath = ""http://" & PortalAlias.HTTPAlias _
   & "/DesktopModules/Blog/ShareBadge/"";</script>" _
   & "<script src=""" & ModulePath & "ShareBadge/js/ShareBadgeChicklets.js""" _
   & "language=""javascript1.2"" type=""text/javascript""></script>"

   Page.ClientScript.RegisterClientScriptBlock(Me.GetType(), "SB_CHICKLETS", SbChickletsScript)


  End If

  'Initialize the main ShareBadge Script
  If Not Page.ClientScript.IsClientScriptBlockRegistered("SB_PRO") Then
   Dim SbProScript As String = "<script src=""" & ModulePath & "ShareBadge/js/ShareBadgePro.js"" language=""javascript1.2"" type=""text/javascript""></script>"
   Page.ClientScript.RegisterClientScriptBlock(Me.GetType(), "SB_PRO", SbProScript)
  End If

  'Initialize the CSS for ShareBadge
  If Not Page.ClientScript.IsClientScriptBlockRegistered("SB_PRO_CSS") Then
   Dim SbProCss As String = "<link rel=""stylesheet"" href=""" & ModulePath & "ShareBadge/css/ShareBadge.css"" type=""text/css"" />"
   Page.ClientScript.RegisterClientScriptBlock(Me.GetType(), "SB_PRO_CSS", SbProCss)
  End If

  'Display the Bookmarks Toolbar
  If Not Page.ClientScript.IsStartupScriptRegistered("SB_PRO_TOOLBAR") Then
   Dim SbToolbar As String = "<script type=""text/javascript"">" _
   & "initializeShareBadge(""ShareBadgePRO_Toolbar"",""" & Replace(EntryTitle, """", "'") & """, """ & EntryUrl & """);" _
   & "addBadgeItem(7);" _
   & "addBadgeItem(33);" _
   & "addBadgeItem(9);" _
   & "addBadgeItem(14);" _
   & "addBadgeItem(17);" _
   & "addBadgeItem(20);" _
   & "addBadgeItem(27);" _
   & "addBadgeItem(28);" _
   & "addBadgeItem(31);" _
   & "addBadgeItem(32);" _
   & "</script>"

   Page.ClientScript.RegisterStartupScript(Me.GetType(), "SB_PRO_TOOLBAR", SbToolbar)
  End If

 End Sub

 Private Sub BindCommentsList()
  Dim objCtlComments As New CommentController
  Dim list As ArrayList

  list = objCtlComments.ListComments(m_oEntry.EntryID, Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, Me.ModuleId))
  lstComments.DataSource = list
  lstComments.DataBind()

  'lblCommentCount.Text = Localization.GetString("msgComments", LocalResourceFile) & lstComments.Items.Count & ")"

  'Antonio Chagoury
  'Using string format routing instead of appending ")"
  lblComments.Text = String.Format(Localization.GetString("lblComments", LocalResourceFile), lstComments.Items.Count)

  txtCommentTitle.Text = Localization.GetString("msgRe", LocalResourceFile) & m_oEntry.Title
  txtComment.Text = ""
  cmdAddComment.Text = Localization.GetString("msgAddComment", LocalResourceFile)
  cmdDeleteComment.Visible = False

  'DR 01/16/2009
  'FIX: BLG-8765
  'Moved this stuff here to populate fields after deleting/updating comments.
  If UserId <> -1 Then
   'Antonio Chagoury 4/09/2007
   'FIX: BLG-3972
   If m_oBlog.ShowFullName Then
    txtAuthor.Text = Me.UserInfo.DisplayName
   Else
    txtAuthor.Text = Me.UserInfo.Username
   End If
  End If
  If UserInfo.UserID > -1 Then
   txtEmail.Text = UserInfo.Email.ToString()
   If Not UserInfo.Profile.GetPropertyValue("Website") Is Nothing Then
    txtWebsite.Text = UserInfo.Profile.GetPropertyValue("Website").ToString
   Else
    txtWebsite.Text = ""
   End If
  End If

 End Sub

 Private Function GetMonth(ByVal strDate As Date, ByVal TimeZone As Integer) As String
  Dim oCultureInfo As New CultureInfo(m_oBlog.Culture)
  Dim MonthFormat As DateTimeFormatInfo = oCultureInfo.DateTimeFormat

  Return MonthFormat.AbbreviatedMonthNames(oCultureInfo.Calendar.GetMonth(strDate.AddMinutes(TimeZone)) - 1)

 End Function

 Private Function GetDay(ByVal strDate As Date, ByVal TimeZone As Integer) As String
  Dim oCultureInfo As New CultureInfo(m_oBlog.Culture)
  Return oCultureInfo.Calendar.GetDayOfMonth(strDate.AddMinutes(TimeZone)).ToString
 End Function

 Private Function GetGravatarUrl(ByVal email As String) As String
  Dim emailHash As String
  Dim gravatarUrl As String
  'Dim gravatarDefaultImageUrl As String = CType(m_oBlogSettings("GravatarDefaultImageUrl"), String)
  If email <> String.Empty Then
   emailHash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(email.ToLower(), "md5").ToLower()
  Else
   emailHash = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(Request.UserHostAddress, "md5").ToLower()
  End If
  gravatarUrl = GravatarURLPrefix() + emailHash + GravatarURLSuffix()
  Return gravatarUrl
 End Function

 Private Function GravatarURLPrefix() As String
  'Get the prefix for the gravatar URL.  This allows us to support 
  ' SSL URLs.
  Dim prefix As String
  If (Request.Url.Scheme = "http") Then
   prefix = "http://www"
  Else
   prefix = "https://secure"
  End If

  Return prefix + ".gravatar.com/avatar/"

 End Function

 Private Function GravatarURLSuffix() As String
  'Retrieve the Module Settings related to Gravatar support
  'Dim gravatarImageWidth As String = CType(m_oBlogSettings("GravatarImageWidth"), String)
  'Dim gravatarRating As String = CType(m_oBlogSettings("GravatarRating"), String)
  'Dim gravatarDefaultImageUrl As String = CType(m_oBlogSettings("GravatarDefaultImageUrl"), String)
  'Dim customImageURL As String = CType(m_oBlogSettings("GravatarCustomUrl"), String)
  'Dim size As String = BlogSettings.GravatarImageWidth
  Dim prefix As String = GravatarURLPrefix()
  Dim defaultImagePath As String

  If BlogSettings.gravatarDefaultImageUrl <> String.Empty Then
   If BlogSettings.gravatarDefaultImageUrl = "custom" Then
    defaultImagePath = BlogSettings.GravatarCustomUrl
   Else
    defaultImagePath = BlogSettings.gravatarDefaultImageUrl
   End If
  Else
   ' First attempt.  Left for reference.  Decided to store default image at Gravatar.com
   ' since the images are cached at varying sizes for you by this service.  email used was
   ' dnnblog@itcrossing.com.  Contact don@itcrossing.com regarding the update of this default
   ' image if an update is needed.  Note.  This is a temporary feature anyway.  When we are able 
   ' to move to .NET 2.0, we'll replace this feature with Identicon support as the default.
   'appPath = HttpContext.Current.Request.ApplicationPath
   'If appPath = "/" Then appPath = String.Empty
   'defaultImagePath = Request.Url.Scheme & "://" & Request.Url.Host & appPath & "/desktopmodules/blog/images/noimage.jpg"
   defaultImagePath = HttpUtility.UrlEncode(String.Format("{0}{1}?size={2}", prefix, "b6267c6fe44b457964aed2218e3ef8fe", BlogSettings.GravatarImageWidth))
  End If

  Return String.Format("?s={0}&r={1}&d={2}", BlogSettings.GravatarImageWidth, BlogSettings.GravatarRating, defaultImagePath)

 End Function

 Private Sub sendMail(ByVal oBlog As BlogInfo, ByVal Comment As CommentInfo)
  Dim strSubject As String = Localization.GetString("msgMailSubject", LocalResourceFile)
  Dim strBody As String = Localization.GetString("msgMailBody", LocalResourceFile)
  ' replace [BLOG] with Blog.Title
  strSubject = strSubject.Replace("[BLOG]", oBlog.Title)
  strBody = strBody.Replace("[BLOG]", oBlog.Title)
  ' replace [TITLE] with Comment.Title
  strSubject = strSubject.Replace("[TITLE]", Comment.Title)
  strBody = strBody.Replace("[TITLE]", Comment.Title)
  ' replace [COMMENT] with Comment.Comment
  strSubject = strSubject.Replace("[COMMENT]", Comment.Comment)
  strBody = strBody.Replace("[COMMENT]", Comment.Comment)
  ' replace [DATE] with Comment.Date
  strSubject = strSubject.Replace("[DATE]", Now.ToShortDateString)
  strBody = strBody.Replace("[DATE]", Now.ToShortDateString)
  ' replace [USER] with Comment.UserName
  strSubject = strSubject.Replace("[USER]", Comment.UserName)
  strBody = strBody.Replace("[USER]", Comment.UserName)
  ' replace [URL] with NavigateUrl()
  strSubject = strSubject.Replace("[URL]", m_oEntry.PermaLink)
  strBody = strBody.Replace("[URL]", m_oEntry.PermaLink)

  Dim uc As New UserController
  Dim ui As UserInfo = uc.GetUser(PortalId, oBlog.UserID)

  DotNetNuke.Services.Mail.Mail.SendMail(PortalSettings.Email, ui.Email, "", strSubject, strBody, "", "html", "", "", "", "")
 End Sub

#End Region

#Region "Public Methods"
 Public Function FormatUserName(ByVal UserName As String) As String
  FormatUserName = String.Format(Localization.GetString("lblFormatUserName.Text", LocalResourceFile), UserName)
 End Function
#End Region

#Region "Web Form Designer Generated Code "

 'This call is required by the Web Form Designer.
 <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()

 End Sub

 'NOTE: The following placeholder declaration is required by the Web Form Designer.
 'Do not delete or move it.
 Private designerPlaceholderDeclaration As System.Object

 Private Sub Page_Init(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Init
  'CODEGEN: This method call is required by the Web Form Designer
  'Do not modify it using the code editor.
  InitializeComponent()

  Me.ModuleConfiguration.DisplayPrint = False
  If Not (Request.Params("EntryID") Is Nothing) Then
   m_oEntryID = Int32.Parse(Request.Params("EntryID"))
   m_oEntry = m_oEntryController.GetEntry(m_oEntryID, PortalId)
   If Not m_oEntry Is Nothing Then
    m_oBlog = m_oBlogController.GetBlog(m_oEntry.BlogID)


    If Not m_oBlog.Public And Not Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, Me.ModuleId) Then
     Response.Redirect(NavigateURL(), True)
     Exit Sub
    End If
   End If
  End If

  'Antonio Chagoury
  'Removing this as I have added a Blog Title Label
  'If Not m_oEntry Is Nothing Then
  '    Me.ModuleConfiguration.ModuleTitle = m_oEntry.Title
  'End If

  If Not m_oEntry Is Nothing Then
   If Utility.HasBlogPermission(Me.UserId, m_oBlog.UserID, Me.ModuleId) Then
    MyActions.Add(GetNextActionID, Localization.GetString("msgEditEntry", LocalResourceFile), "", Url:=EditUrl("EntryID", m_oEntry.EntryID.ToString(), "Edit_Entry"), Secure:=DotNetNuke.Security.SecurityAccessLevel.Edit, Visible:=True)
    lnkEditEntry.Visible = True
    lnkEditEntry.NavigateUrl = EditUrl("EntryID", m_oEntry.EntryID.ToString(), "Edit_Entry")
   Else
    lnkEditEntry.Visible = False
   End If
  End If
  Me.ModuleConfiguration.SupportedFeatures = 0
 End Sub

#End Region

End Class

