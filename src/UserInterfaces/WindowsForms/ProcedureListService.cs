#region License
/* 
 * Copyright (C) 1999-2022 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using Reko.Gui.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reko.Gui
{
    public class ProcedureListService : IProcedureListService
    {
        private const string HelpText = "Search procedures";
        private const int icolAddress = 0;
        private const int icolName = 1;
        private const int icolSegment = 2;

        private readonly IServiceProvider services;
        private readonly TabPage tabPage;
        private readonly TextBox txtProcedureFilter;
        private readonly ListView listProcedures;
        private bool showHelpText;
        private List<ProgramProcedure> procs;

        public ProcedureListService(IServiceProvider services, TabPage tabPage, TextBox txtProcedureFilter, ListView listProcedures)
        {
            this.services = services;
            this.tabPage = tabPage;
            this.txtProcedureFilter = txtProcedureFilter;
            this.listProcedures = listProcedures;
            this.showHelpText = true;
            txtProcedureFilter.GotFocus += TxtProcedureFilter_GotFocus;
            txtProcedureFilter.LostFocus += TxtProcedureFilter_LostFocus;
            txtProcedureFilter.TextChanged += TxtProcedureFilter_TextChanged;
            listProcedures.DoubleClick += ListProcedures_DoubleClick;
            services.RequireService<IDecompilerShellUiService>().SetContextMenu(listProcedures, Reko.Gui.MenuIds.CtxProcedureList);
            Clear();
        }

        public bool ContainsFocus
        {
            get { return this.txtProcedureFilter.Focused || this.listProcedures.Focused;  }
        }

        public void Clear()
        {
            procs = new List<ProgramProcedure>();
            this.showHelpText = true;
            this.txtProcedureFilter.Text = HelpText;
            this.txtProcedureFilter.ForeColor = System.Drawing.SystemColors.GrayText;
            ShowProcedures(new ProgramProcedure[0]);
        }

        public void Load(Project project)
        {
            this.procs = project.Programs
                .SelectMany(program => program.Procedures.Values
                    .Select(p => new ProgramProcedure { Program = program, Procedure = p } ))
                .ToList();
            if (procs.Count == 0)
            {
                Clear();
                return;
            }
            ShowProcedures(procs);
        }

        private (string,string) StringizeProcedure(Procedure proc)
        {
            return (proc.EntryAddress.ToString(), proc.Name);
        }

        private void ShowProcedures(IEnumerable<ProgramProcedure> sProcs)
        {
            var items = sProcs.Select(CreateListItem).ToArray();
            if (items.Length == 0)
            {
                items = new[] { new ListViewItem(new[] { "(None)", "", "" }) };
            }
            this.listProcedures.Items.Clear();
            this.listProcedures.Items.AddRange(items);
        }

        private ListViewItem CreateListItem(ProgramProcedure item)
        {
            string[] subItems = CreateListItemTexts(item);
            return new ListViewItem(subItems)
            {
                Tag = item
            };
        }

        private static string[] CreateListItemTexts(ProgramProcedure item)
        {
            var segName = item.Program.SegmentMap.TryFindSegment(item.Procedure.EntryAddress, out var seg)
                ? seg.Name
                : "???";
            var subItems = new[] {
                item.Procedure.EntryAddress.ToString(),
                item.Procedure.Name,
                segName
            };
            return subItems;
        }

        public bool QueryStatus(CommandID cmdId, CommandStatus status, CommandText text)
        {
            int cItems = listProcedures.SelectedItems.Count;
            var singleItemSelected = cItems == 1;
            ProgramProcedure pp = default;
            if (singleItemSelected && listProcedures.SelectedItems[0].Tag is not null)
            {
                pp = (ProgramProcedure) listProcedures.SelectedItems[0].Tag;
            }
            if (cmdId.Guid == CmdSets.GuidReko) 
            {
                switch ((CmdIds)cmdId.ID)
                {
                case CmdIds.ActionEditSignature:
                case CmdIds.ViewGoToAddress:
                case CmdIds.ShowProcedureCallHierarchy:
                    status.Status = singleItemSelected
                        ? MenuStatus.Enabled | MenuStatus.Visible
                        : MenuStatus.Visible;
                    return true;
#if DEBUG
                case CmdIds.ProcedureDebugTrace:
                    var st = MenuStatus.Visible;
                    if (singleItemSelected)
                    {
                        if (pp.Program.User.DebugTraceProcedures.Contains(pp.Procedure.Name))
                            st |= MenuStatus.Checked;
                        if (singleItemSelected)
                            st |= MenuStatus.Enabled;
                    }
                    status.Status = st;
                    return true;
#endif
                }
            }
            return false;
        }

        public async ValueTask<bool> ExecuteAsync(CommandID cmdId)
        {
            if (cmdId.Guid == CmdSets.GuidReko)
            {
                switch ((CmdIds)cmdId.ID)
                {
                case CmdIds.ActionEditSignature:
                    await EditProcedureSignature();
                    return true;
                case CmdIds.ViewGoToAddress:
                    GotoProcedureAddress();
                    return true;
                case CmdIds.ShowProcedureCallHierarchy:
                    ShowProcedureCallHierarchy();
                    return true;
#if DEBUG
                case CmdIds.ProcedureDebugTrace:
                    DebugTraceSelectedProcedure();
                    return true;
#endif

                }
            }
            return false;
        }

        public void Show()
        {
            ((TabControl) tabPage.Parent).SelectedTab = tabPage;
        }

        private async ValueTask EditProcedureSignature()
        {
            Debug.Assert(listProcedures.SelectedItems.Count == 1, "QueryStatus should be ensuring this");
            var item = listProcedures.SelectedItems[0];
            if (item.Tag is ProgramProcedure pp)
            {
                await services.RequireService<ICommandFactory>().EditSignature(pp.Program, pp.Procedure, pp.Procedure.EntryAddress).DoAsync();
                UpdateItem(item);
            }
        }

        private void GotoProcedureAddress()
        {
            Debug.Assert(listProcedures.SelectedItems.Count == 1, "QueryStatus should be ensuring this");
            var item = listProcedures.SelectedItems[0];
            if (item.Tag is ProgramProcedure pp)
            {
                services.RequireService<ILowLevelViewService>().ShowMemoryAtAddress(pp.Program, pp.Procedure.EntryAddress);
            }
        }

        private void ShowProcedureCallHierarchy()
        {
            Debug.Assert(listProcedures.SelectedItems.Count == 1, "QueryStatus should be ensuring this");
            var item = listProcedures.SelectedItems[0];
            if (item.Tag != null)
            {
                var pp = (ProgramProcedure) item.Tag;
                services.RequireService<ICallHierarchyService>().Show(pp.Program, pp.Procedure);
            }
        }

        private IEnumerable<ProgramProcedure> SelectedItems()
        {
            return listProcedures.SelectedItems
                .OfType<ListViewItem>()
                .Select(i => (ProgramProcedure)i.Tag);
        }

#if DEBUG
        private void DebugTraceSelectedProcedure()
        {
            var item = listProcedures.SelectedItems[0];
            if (item.Tag != null)
            {
                var pp = (ProgramProcedure) item.Tag;
                var name = pp.Procedure.Name;
                var procs = pp.Program.User.DebugTraceProcedures;
                if (procs.Contains(name))
                    procs.Remove(name);
                else
                    procs.Add(name);
            }
        }
#endif

            private void UpdateItem(ListViewItem item)
        {
            if (item.Tag == null)
                return;

            var subItems = CreateListItemTexts((ProgramProcedure) item.Tag);
            for (int i = 0; i < subItems.Length; ++i)
            {
                item.SubItems[i].Text = subItems[i];
            }
        }

        private void TxtProcedureFilter_LostFocus(object sender, EventArgs e)
        {
            if (!this.showHelpText && txtProcedureFilter.Text.Length == 0)
            {
                this.showHelpText = true;
                this.txtProcedureFilter.Text = HelpText;
                this.txtProcedureFilter.ForeColor = SystemColors.GrayText;
            }
        }

        private void TxtProcedureFilter_GotFocus(object sender, EventArgs e)
        {
            if (this.showHelpText)
            {
                this.txtProcedureFilter.Text = "";
                this.showHelpText = false;
                this.txtProcedureFilter.ForeColor = SystemColors.ControlText;
            }
        }

        private void TxtProcedureFilter_TextChanged(object sender, EventArgs e)
        {
            if (showHelpText)
                return;
            var searchText = txtProcedureFilter.Text.Trim();
            IEnumerable<ProgramProcedure> procs = this.procs;
            if (!string.IsNullOrEmpty(searchText))
            {
                procs = procs.Where(sItem => FilterProcedure(searchText, sItem));
            }
            ShowProcedures(procs);
        }

        private bool FilterProcedure(string searchtext, ProgramProcedure sItem)
        {
            return (sItem.Procedure.EntryAddress.ToString().Contains(searchtext) || 
                sItem.Procedure.Name.Contains(searchtext));
        }

        private void ListProcedures_DoubleClick(object sender, EventArgs e)
        {
            var item = listProcedures.FocusedItem;
            if (item == null || !item.Selected || item.Tag is not ProgramProcedure pp)
                return;
            services.RequireService<ICodeViewerService>().DisplayProcedure(pp.Program, pp.Procedure, pp.Program.NeedsScanning);
        }

        private struct ProgramProcedure
        {
            public Program Program;
            public Procedure Procedure;
        }
    }

}
