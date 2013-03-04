<%@ Page Language="C#" MasterPageFile="~/Main.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Website.Aion.RecipeCalculator" EnableViewState="false" %>

<%--<%@ OutputCache CacheProfile="Long" VaryByParam="lang" %>--%>

<asp:Content ID="Content1" ContentPlaceHolderID="cD" runat="server">
	<div id="RecipeCalculator_aspx">
		<table style="width: 100%">
			<tr>
				<td>
					<input class="addRecipe" type="button" value="<%= Resources.AionRecipeCalculator.s115 %>" style="height: 30px" />
                    <button tooltip="<%= Resources.AionRecipeCalculator.s38 %>" style="height: 30px" onclick="location.href=<%= this.Request.RawUrl %>"><%= Resources.AionRecipeCalculator.s39 %></button>
				</td>
				<td class="topMenu">
					<asp:Literal runat="server" ID="langLinks" Mode="PassThrough"></asp:Literal>
					<span style="padding-left: 50px"></span>
                    <a href="javascript:;" class="reset" tooltip="<%= Resources.AionRecipeCalculator.s113 %>"><%= Resources.AionRecipeCalculator.s114 %></a> | 
                    <a href="<%= this.Request.RawUrl %>" tooltip="<%= Resources.AionRecipeCalculator.s38 %>"><%= Resources.AionRecipeCalculator.s39 %></a> | 
                    <a href="javascript:;" class="feedback" tooltip="<%= Resources.AionRecipeCalculator.s34 %>"><%= Resources.AionRecipeCalculator.s35 %></a>
				</td>
			</tr>
		</table>
		<br />
		<br />
		<br />
		<div class="ph phEmpty">
            <h3><strong style="color: blue;">Notice:</strong> Icon images are now fixed. Tooltips are disabled due to no syndication tool available that fit our needs.</h3><br /><br />
            
            <h3><%= Resources.AionRecipeCalculator.s40 %></h3>
            <br />
            
            <!-- // MAILCHIMP SUBSCRIBE CODE \\ -->
            <h3><a href="http://eepurl.com/nZNob">Subscribe to our Newsletter</a></h3>
            <!-- \\ MAILCHIMP SUBSCRIBE LINK // -->
			<br />
            <%= Resources.AionRecipeCalculator.s117 %> <%= Resources.AionRecipeCalculator.s119 %>
            <br /><br />
            <a href="<%= Resources.AionRecipeCalculator.s123 %>" target="_blank"><img src="<%= Resources.AionRecipeCalculator.s118 %>" alt=""></a>
            
            <div style="float: right;">
                <script type="text/javascript">
                    ch_client = "diegovieira";
                    ch_width = 728;
                    ch_height = 90;
                    ch_type = "mpu";
                    ch_sid = "Aion Calculator";
                    ch_color_site_link = "CC0000";
                    ch_color_title = "CC0000";
                    ch_color_border = "FFFFFF";
                    ch_color_text = "000000";
                    ch_color_bg = "FFFFFF";
                </script>
                <script src="http://scripts.chitika.net/eminimalls/amm.js" type="text/javascript">
                </script>
            </div>
            <br style="clear: both;" />
		</div>
		<div class="ph phDefault dn">
			<table style="width: 100%">
				<tr>
					<td class="recipePane">
						<div class="recipes">
							<h2>
								<a href="javascript:;" class="recipeLink openDatabaseLink" recipeid="0" style="position: relative; top: -5px" target="_blank" tooltip="<%= Resources.AionRecipeCalculator.s42 %>">
									<img src="Images/arrow-045-small.png" alt="" /></a> <a href="javascript:;" class="recipeInfoLink viewInformation" recipeid="0" style="position: relative; top: -5px; left: -5px" target="_blank" tooltip="<%= Resources.AionRecipeCalculator.s43 %>">
										<img src="Images/information-small.png" alt="" /></a>
							</h2>
							<br />
							<div class="result">
							</div>
						</div>
					</td>
					<td class="resultsPane">
						<div class="total">
						</div>
					</td>
				</tr>
			</table>
			<br />
		</div>
		<div class="comment" style="padding-top: 30px">
			<%= Resources.AionRecipeCalculator.s120 %> <%=fileUpdate()%> - <%= Resources.AionRecipeCalculator.s121 %> 3.0 -
            <%= Resources.AionRecipeCalculator.s44 %>
			<a href="javascript:;" class="feedback comment">
				<%= Resources.AionRecipeCalculator.s45 %></a> - support@aioncalculator.com
		</div>
        <div style="margin-top: 10px">
        <script id="_wauzuu">            var _wau = _wau || [];
            _wau.push(["colored", "4h60f8650gss", "zuu", "ffffff000000"]);
            (function () {
                var s = document.createElement("script"); s.async = true;
                s.src = "http://widgets.amung.us/colored.js";
                document.getElementsByTagName("head")[0].appendChild(s);
            })();</script>
        </div>
	</div>
	<script type="text/javascript">
		var Lang = '<%= this.currentLanguage.Name %>';
		var Version = '<%= this.version %>';
	</script>
	<script type="text/javascript" src="AionLocalizationJs.axd?l=<%= this.currentLanguage.Name %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.Config.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.Parameters.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.Recipe.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.Item.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.SelectItemDialog.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.Utils.js?v=<%= this.version %>"></script>
	<script type="text/javascript" src="Scripts/RecipeCalculator.js?v=<%= this.version %>"></script>
</asp:Content>
