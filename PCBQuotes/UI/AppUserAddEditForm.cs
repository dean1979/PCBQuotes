﻿using PCBQuotes.Helpers;
using PCBQuotes.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace PCBQuotes.UI
{
    public partial class AppUserAddEditForm : Telerik.WinControls.UI.RadForm
    {
        private readonly object obj = new object();//用于线程锁对象
        private SynchronizationContext syncContext = SynchronizationContext.Current;//线程中更新UI模型传播上下文
        private BLL.GeneralBll bll = new BLL.GeneralBll();
        public Models.AppUser DataEntry { get; set; }
        public Enums.EditFormMode EditMode { get; set; }

        public AppUserAddEditForm()
        {
            InitializeComponent();
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnCancel;
            this.FormClosed += (s, e) => {
                //释放 资源 
                if (bll != null)
                {
                    bll.Dispose();
                    bll = null;
                }
            };
            //设置dataentity
            this.deMain.FitToParentWidth = true;
            this.deMain.ShowValidationPanel = true;
            this.deMain.EditorInitializing += (s, e) => {
                e.Editor.Name = e.Property.Name;
                if (e.Property.Name=="UserName"&&EditMode== Enums.EditFormMode.Edit)
                {
                    e.Editor.Enabled = false;
                }
                else if(e.Property.Name=="Email")
                {
                    RadMaskedEditBox editor = new RadMaskedEditBox();
                    editor.MaskType = MaskType.EMail;
                    editor.MaskedEditBoxElement.StretchVertically = true;
                    e.Editor = editor;
                }
                //else if (e.Property.Name == "Tel")
                //{
                //    RadMaskedEditBox editor = new RadMaskedEditBox();
                //    editor.MaskType = MaskType.Regex;
                //    editor.MaskedEditBoxElement.StretchVertically = true;
                //    //editor.Mask = "9";
                //    e.Editor = editor;
                //}
            };
            this.deMain.ItemValidating += (s, e) => {
                Models.AppUser user = this.deMain.CurrentObject as Models.AppUser;
                switch (e.Label.Text)
                {
                    case "用户名":
                        if (string.IsNullOrWhiteSpace(user.UserName))
                        {
                            string errorMessage = "用户名不能为空！";
                            e.ErrorProvider.SetError(s as Control, errorMessage);
                            e.Cancel = true;
                            AddErrorLabel(this.deMain, e.Label.Text, errorMessage);
                        }
                        else
                        {
                            e.ErrorProvider.Clear();
                            this.deMain.ValidationPanel.PanelContainer.Controls.RemoveByKey(e.Label.Text);
                        }
                        break;
                    default:
                        break;
                }
            };

            this.Shown += (s, e) => {
                //设置窗体title
                switch (EditMode)
                {
                    case Enums.EditFormMode.Add:
                        this.Text = "新增";
                        DataEntry = new Models.AppUser() { Password=Helpers.EncryptDecryptHelper.GetStringMD5("888888")};//新增用户默认密码
                        break;
                    case Enums.EditFormMode.Edit:
                        this.Text = "编辑";
                        break;
                    case Enums.EditFormMode.Delete:
                        this.Text = "删除";
                        break;
                    default:
                        this.Text = "新增/编辑";
                        break;
                }
                if (DataEntry != null)
                {
                    this.deMain.DataSource = this.DataEntry;
                    
                }
            };

            this.btnCancel.Click += (s, e) => {
                //this.UserRole.CancelEdit();
                this.DialogResult = DialogResult.Cancel;
            };

            this.btnOk.Click += (s, e) => {
                var t = ValidationHelper.hasValidationErrors(this.deMain.Controls);
                if (t)
                {
                    return;
                }

                this.btnOk.Enabled = false;
                
                Task.Factory.StartNew<AppUser>(() => {
                    Models.AppUser re = null;
                    if (EditMode == Enums.EditFormMode.Add)
                    {
                        re = bll.Insert<Models.AppUser>(this.DataEntry);
                    }
                    else if (EditMode == Enums.EditFormMode.Edit)
                    {
                        re = bll.Update<Models.AppUser>(this.DataEntry);
                    }

                    
                    return re;
                }).ContinueWith(tt=> {
                    if (!tt.IsFaulted)
                    {
                        syncContext.Post((state) => {
                            
                            Models.AppUser sta = (Models.AppUser)state;
                            if (sta != null)
                            {
                                this.DataEntry = sta;
                                this.DialogResult = DialogResult.OK;
                                //this.SubmitSucess = true;
                            }
                            else
                            {
                                RadMessageBox.Show(this, "保存失败!", "", MessageBoxButtons.OK, RadMessageIcon.Error);
                            }
                        }, tt.Result);
                    }
                    syncContext.Post((state) => {
                        this.btnOk.Enabled = true;
                         
                    }, tt.Result); 
                });
            };
        }
        private void AddErrorLabel(RadDataEntry dataEntry, string propertyName, string errorMessage)
        {
            if (!dataEntry.ValidationPanel.PanelContainer.Controls.ContainsKey(propertyName))
            {
                RadLabel label = new RadLabel();
                label.Name = propertyName;
                label.Text = "" + propertyName + " : " + errorMessage;
                label.Dock = DockStyle.Top;
                //label.MaximumSize = new System.Drawing.Size(480, 0);
                label.TextWrap = true;
                dataEntry.ValidationPanel.PanelContainer.Controls.Add(label);
            }
        }
    }
}
