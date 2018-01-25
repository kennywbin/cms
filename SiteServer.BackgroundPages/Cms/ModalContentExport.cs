﻿using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using SiteServer.Utils;
using SiteServer.BackgroundPages.Controls;
using SiteServer.CMS.Core;
using SiteServer.Utils.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
	public class ModalContentExport : BasePageCms
    {
        public DropDownList DdlExportType;
        public DropDownList DdlPeriods;
        public DateTimeTextBox TbStartDate;
        public DateTimeTextBox TbEndDate;
        public PlaceHolder PhDisplayAttributes;
        public CheckBoxList CblDisplayAttributes;
        public DropDownList DdlIsChecked;

        private int _nodeId;

        public static string GetOpenWindowString(int siteId, int nodeId)
        {
            return LayerUtils.GetOpenScriptWithCheckBoxValue("导出内容",
                PageUtils.GetCmsUrl(siteId, nameof(ModalContentExport), new NameValueCollection
                {
                    {"NodeID", nodeId.ToString()}
                }), "ContentIDCollection", string.Empty);
        }

        private void LoadDisplayAttributeCheckBoxList()
        {
            var nodeInfo = ChannelManager.GetChannelInfo(SiteId, _nodeId);
            var relatedIdentities = RelatedIdentities.GetChannelRelatedIdentities(SiteId, _nodeId);
            var tableName = ChannelManager.GetTableName(SiteInfo, nodeInfo);
            var styleInfoList = TableStyleManager.GetTableStyleInfoList(tableName, relatedIdentities);
            styleInfoList = ContentUtility.GetAllTableStyleInfoList(SiteInfo, styleInfoList);

            foreach (var styleInfo in styleInfoList)
            {
                var listItem = new ListItem(styleInfo.DisplayName, styleInfo.AttributeName)
                {
                    Selected = true
                };
                CblDisplayAttributes.Items.Add(listItem);
            }
        }

		public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            _nodeId = Body.GetQueryInt("NodeID", SiteId);
            if (IsPostBack) return;

            LoadDisplayAttributeCheckBoxList();
            ConfigSettings(true);
        }

        private void ConfigSettings(bool isLoad)
        {
            if (isLoad)
            {
                if (!string.IsNullOrEmpty(SiteInfo.Additional.ConfigExportType))
                {
                    DdlExportType.SelectedValue = SiteInfo.Additional.ConfigExportType;
                }
                if (!string.IsNullOrEmpty(SiteInfo.Additional.ConfigExportPeriods))
                {
                    DdlPeriods.SelectedValue = SiteInfo.Additional.ConfigExportPeriods;
                }
                if (!string.IsNullOrEmpty(SiteInfo.Additional.ConfigExportDisplayAttributes))
                {
                    var displayAttributes = TranslateUtils.StringCollectionToStringList(SiteInfo.Additional.ConfigExportDisplayAttributes);
                    ControlUtils.SelectMultiItems(CblDisplayAttributes, displayAttributes);
                }
                if (!string.IsNullOrEmpty(SiteInfo.Additional.ConfigExportIsChecked))
                {
                    DdlIsChecked.SelectedValue = SiteInfo.Additional.ConfigExportIsChecked;
                }
            }
            else
            {
                SiteInfo.Additional.ConfigExportType = DdlExportType.SelectedValue;
                SiteInfo.Additional.ConfigExportPeriods = DdlPeriods.SelectedValue;
                SiteInfo.Additional.ConfigExportDisplayAttributes = ControlUtils.GetSelectedListControlValueCollection(CblDisplayAttributes);
                SiteInfo.Additional.ConfigExportIsChecked = DdlIsChecked.SelectedValue;
                DataProvider.SiteDao.Update(SiteInfo);
            }
        }

        public void DdlExportType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PhDisplayAttributes.Visible = DdlExportType.SelectedValue != "ContentZip";
        }

        public override void Submit_OnClick(object sender, EventArgs e)
        {
            var displayAttributes = ControlUtils.GetSelectedListControlValueCollection(CblDisplayAttributes);
            if (PhDisplayAttributes.Visible && string.IsNullOrEmpty(displayAttributes))
            {
                FailMessage("必须至少选择一项！");
                return;
            }

            ConfigSettings(false);

            var isPeriods = false;
            var startDate = string.Empty;
            var endDate = string.Empty;
            if (DdlPeriods.SelectedValue != "0")
            {
                isPeriods = true;
                if (DdlPeriods.SelectedValue == "-1")
                {
                    startDate = TbStartDate.Text;
                    endDate = TbEndDate.Text;
                }
                else
                {
                    var days = int.Parse(DdlPeriods.SelectedValue);
                    startDate = DateUtils.GetDateString(DateTime.Now.AddDays(-days));
                    endDate = DateUtils.GetDateString(DateTime.Now);
                }
            }
            var checkedState = ETriStateUtils.GetEnumType(DdlPeriods.SelectedValue);
            var redirectUrl = ModalExportMessage.GetRedirectUrlStringToExportContent(SiteId, _nodeId, DdlExportType.SelectedValue, Body.GetQueryString("ContentIDCollection"), displayAttributes, isPeriods, startDate, endDate, checkedState);
            PageUtils.Redirect(redirectUrl);
		}
	}
}