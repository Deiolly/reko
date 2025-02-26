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
using Reko.Core.Configuration;
using Reko.Core.Loading;
using Reko.Core.Memory;
using Reko.Core.Serialization;
using Reko.Core.Services;
using Reko.Core.Types;
using Reko.Gui.Components;
using Reko.Gui.Services;
using Reko.Scanning;
using Reko.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reko.Gui.Forms
{
    /// <summary>
    /// Provides a component Container implementation, and specifically handles interactions 
    /// with the MainForm. This decouples platform-specific code from the user interaction 
    /// code. This will make it easier to port to other GUI platforms.
    /// </summary>
    public class MainFormInteractor :
        ReactingObject,
        ICommandTarget
    {
        private IDecompilerShellUiService uiSvc;
        private IMainForm form;
        private IDecompilerService decompilerSvc;
        private IDiagnosticsService diagnosticsSvc;
        private ISearchResultService srSvc;
        private IWorkerDialogService workerDlgSvc;
        private IProjectBrowserService projectBrowserSvc;
        private IProcedureListService procedureListSvc;
        private IDialogFactory dlgFactory;
        private ITabControlHostService searchResultsTabControl;
        private ILoader loader;

        private IPhasePageInteractor currentPhase;
        private InitialPageInteractor pageInitial;
        private IScannedPageInteractor pageScanned;
        private IAnalyzedPageInteractor pageAnalyzed;
        private IFinalPageInteractor pageFinal;

        private MruList mru;
        private IServiceContainer sc;
        private ProjectFilesWatcher projectFilesWatcher;
        private IConfigurationService config;
        private ICommandTarget subWindowCommandTarget;

        private static string? dirSettings;     //$REFACTOR: this belongs elsewhere

        private const int MaxMruItems = 9;

#nullable disable
        public MainFormInteractor(IServiceProvider services)
        {
            this.dlgFactory = services.RequireService<IDialogFactory>();
            this.mru = new MruList(MaxMruItems);
            this.mru.Load(services.RequireService<IFileSystemService>(), MruListFile);
            this.sc = services.RequireService<IServiceContainer>();
            this.subWindowCommandTarget = new NullCommandTarget();
        }
#nullable enable

        public IServiceProvider Services => sc;

        public string? ProjectFileName { get; set; }

        public string TitleText
        {
            get { return titleText; }
            set { RaiseAndSetIfChanged(ref titleText, value); }
        }
        private string titleText;

        public void Attach(IMainForm mainForm)
        {
            this.form = mainForm;

            uiSvc = sc.RequireService<IDecompilerShellUiService>();
            subWindowCommandTarget = uiSvc;

            var svcFactory = sc.RequireService<IServiceFactory>();
            CreateServices(svcFactory, sc);
            CreatePhaseInteractors(svcFactory);
            projectBrowserSvc.Clear();
            this.projectFilesWatcher = new ProjectFilesWatcher(sc);

            var uiPrefsSvc = sc.RequireService<IUiPreferencesService>();
            // It's ok if we can't load settings, just proceed with defaults.
            try
            {
                uiPrefsSvc.Load();
                if (uiPrefsSvc.WindowSize != new System.Drawing.Size())
                    form.Size = uiPrefsSvc.WindowSize;
                form.WindowState = uiPrefsSvc.WindowState;
            }
            catch { };

            this.currentPhase = pageInitial;
            form.UpdateToolbarState();

            form.Closed += this.MainForm_Closed;

            //form.InitialPage.IsDirtyChanged += new EventHandler(InitialPage_IsDirtyChanged);//$REENABLE
            //MainForm.InitialPage.IsDirty = false;         //$REENABLE

            UpdateWindowTitle();
        }

        private void CreatePhaseInteractors(IServiceFactory svcFactory)
        {
            pageInitial =  svcFactory.CreateInitialPageInteractor();
            pageScanned = svcFactory.CreateScannedPageInteractor();
            pageAnalyzed = new AnalyzedPageInteractorImpl(sc);
            pageFinal = new FinalPageInteractor(sc);
        }

        private void CreateServices(IServiceFactory svcFactory, IServiceContainer sc)
        {
            config = svcFactory.CreateDecompilerConfiguration();
            sc.AddService(typeof(IConfigurationService), config);

            var cmdFactory = new Commands.CommandFactory(sc);
            sc.AddService<ICommandFactory>(cmdFactory);

            var sbSvc = svcFactory.CreateStatusBarService();
            sc.AddService<IStatusBarService>(sbSvc);

            diagnosticsSvc = svcFactory.CreateDiagnosticsService();
            sc.AddService(typeof(IDiagnosticsService), diagnosticsSvc);

            decompilerSvc = svcFactory.CreateDecompilerService();
            sc.AddService(typeof(IDecompilerService), decompilerSvc);

            sc.AddService(typeof(IDecompilerUIService), uiSvc);

            var codeViewSvc = svcFactory.CreateCodeViewerService();
            sc.AddService<ICodeViewerService>(codeViewSvc);

            var textEditorSvc = svcFactory.CreateTextFileEditorService();
            sc.AddService<ITextFileEditorService>(textEditorSvc);

            var segmentViewSvc = svcFactory.CreateImageSegmentService();
            sc.AddService(typeof(ImageSegmentService), segmentViewSvc);

            var del = svcFactory.CreateDecompilerEventListener();
            workerDlgSvc = (IWorkerDialogService)del;
            sc.AddService(typeof(IWorkerDialogService), workerDlgSvc);
            sc.AddService<DecompilerEventListener>(del);

            sc.AddService<IDecompiledFileService>(svcFactory.CreateDecompiledFileService());

            loader = svcFactory.CreateLoader();
            sc.AddService<ILoader>(loader);

            var abSvc = svcFactory.CreateArchiveBrowserService();
            sc.AddService<IArchiveBrowserService>(abSvc);

            sc.AddService<ILowLevelViewService>(svcFactory.CreateMemoryViewService());
            sc.AddService<IDisassemblyViewService>(svcFactory.CreateDisassemblyViewService());

            var tlSvc = svcFactory.CreateTypeLibraryLoaderService();
            sc.AddService<ITypeLibraryLoaderService>(tlSvc);

            this.projectBrowserSvc = svcFactory.CreateProjectBrowserService();
            sc.AddService<IProjectBrowserService>(projectBrowserSvc);

            this.procedureListSvc = svcFactory.CreateProcedureListService();
            sc.AddService<IProcedureListService>(procedureListSvc);

            var upSvc = svcFactory.CreateUiPreferencesService();
            sc.AddService<IUiPreferencesService>(upSvc);

            srSvc = svcFactory.CreateSearchResultService();
            sc.AddService<ISearchResultService>(srSvc);

            var callHierSvc = svcFactory.CreateCallHierarchyService();
            sc.AddService<ICallHierarchyService>(callHierSvc);

            this.searchResultsTabControl = svcFactory.CreateTabControlHost();
            sc.AddService<ITabControlHostService>(this.searchResultsTabControl);

            var resEditService = svcFactory.CreateResourceEditorService();
            sc.AddService<IResourceEditorService>(resEditService);

            var cgvSvc = svcFactory.CreateCallGraphViewService();
            sc.AddService<ICallGraphViewService>(cgvSvc);

            var viewImpSvc = svcFactory.CreateViewImportService();
            sc.AddService<IViewImportsService>(viewImpSvc);

            var symLdrSvc = svcFactory.CreateSymbolLoadingService();
            sc.AddService<ISymbolLoadingService>(symLdrSvc);

            var selSvc = svcFactory.CreateSelectionService();
            sc.AddService<ISelectionService>(selSvc);

            var selAddrSvc = svcFactory.CreateSelectedAddressService();
            sc.AddService<ISelectedAddressService>(selAddrSvc);

            var testGenSvc = svcFactory.CreateTestGenerationService();
            sc.AddService<ITestGenerationService>(testGenSvc);

            var userEventSvc = svcFactory.CreateUserEventService();
            sc.AddService<IUserEventService>(userEventSvc);

            var outputSvc = svcFactory.CreateOutputService();
            sc.AddService<IOutputService>(outputSvc);

            var stackTraceSvc = svcFactory.CreateStackTraceService();
            sc.AddService<IStackTraceService>(stackTraceSvc);

            var hexDasmSvc = svcFactory.CreateHexDisassemblerService();
            sc.AddService<IHexDisassemblerService>(hexDasmSvc);

            var segListSvc = svcFactory.CreateSegmentListService();
            sc.AddService<ISegmentListService>(segListSvc);
        }

        public virtual TextWriter CreateTextWriter(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return StreamWriter.Null;
            var fsSvc = Services.RequireService<IFileSystemService>();
            var dir = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir))
                fsSvc.CreateDirectory(dir);
            return new StreamWriter(fsSvc.CreateFileStream(filename, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
        }

        public IPhasePageInteractor CurrentPhase
        {
            get { return currentPhase; }
            set { currentPhase = value; }
        }

        public async ValueTask OpenBinaryWithPrompt()
        {
            var uiSvc = Services.RequireService<IDecompilerShellUiService>();
            var fileName = await uiSvc.ShowOpenFileDialog(null);
            if (fileName is not null)
            {
                RememberFilenameInMru(fileName);
                await CloseProject();
                await SwitchInteractor(InitialPageInteractor);
                if (!await pageInitial.OpenBinary(fileName))
                    return;
                if (fileName.EndsWith(Project_v5.FileExtension))
                {
                    ProjectFileName = fileName;
                }
            }
        }

        private void RememberFilenameInMru(string fileName)
        {
            mru.Use(fileName);
            mru.Save(Services.RequireService<IFileSystemService>(), MruListFile);
        }

        /// <summary>
        /// Prompts the user for a metadata file and adds to the project.
        /// </summary>
        public async ValueTask AddMetadataFile()
        {
            var fileName = await uiSvc.ShowOpenFileDialog(null);
            if (fileName is null)
                return;
            var projectLoader = new ProjectLoader(
                Services,
                loader,
                this.decompilerSvc.Decompiler!.Project!,
                Services.RequireService<DecompilerEventListener>());
            var metadataUri = ImageLocation.FromUri(fileName);
            try
            {
                var metadata = projectLoader.LoadMetadataFile(metadataUri);
                decompilerSvc.Decompiler.Project!.MetadataFiles.Add(metadata);
                RememberFilenameInMru(fileName);
            }
            catch (Exception e)
            {
                await uiSvc.ShowError(e, "An error occured while parsing the metadata file {0}", fileName);
            }
        }

        /// <summary>
        /// Prompts the user for a script file and adds to the project.
        /// </summary>
        private async ValueTask AddScriptFile()
        {
            var fileName = await uiSvc.ShowOpenFileDialog(null);
            if (fileName is null)
                return;
            await AddScriptFile(fileName);
        }

        /// <summary>
        /// Prompts the user for a creating new script file and adds it to the
        /// project.
        /// </summary>
        private async ValueTask CreateScriptFile()
        {
            var fileName = await uiSvc.ShowSaveFileDialog(GetDefaultScriptPath());
            if (fileName is null)
                return;
            var fsSvc = sc.RequireService<IFileSystemService>();
            try
            {
                fsSvc.CopyFile(GetScriptTemplatePath(), fileName, true);
                await AddScriptFile(fileName);
            }
            catch (Exception e)
            {
               await uiSvc.ShowError(
                    e,
                    "An error occured while creating the script file {0}.",
                    fileName);
            }
        }

        private string? GetDefaultScriptPath()
        {
            var defaultFileName = "new_script.py";
            return ProjectPersister.ConvertToAbsolutePath(
                ProjectFileName!, defaultFileName);
        }

        private string GetScriptTemplatePath()
        {
            var cfgSvc = sc.RequireService<IConfigurationService>();
            var templDir = "Python";
            var templName = "_new_script_template.py";
            return cfgSvc.GetInstallationRelativePath(templDir, templName);
        }

        /// <summary>
        /// Adds scripts file to the project.
        /// </summary>
        private async ValueTask AddScriptFile(string fileName)
        {
            try
            {
                var scriptLocation = ImageLocation.FromUri(fileName);
                var script = loader.LoadScript(scriptLocation);
                if (script is null)
                    return;
                var project = decompilerSvc.Decompiler!.Project;
                if (project is null)
                    return;
                project.ScriptFiles.Add(script);
            }
            catch (Exception e)
            {
                await uiSvc.ShowError(
                    e,
                    "An error occured while parsing the script file {0}.",
                    fileName);
            }
        }

        public async ValueTask<bool> AssembleFile()
        {
            using IAssembleFileDialog dlg = dlgFactory.CreateAssembleFileDialog();
            dlg.Services = sc;
            if (await uiSvc.ShowModalDialog(dlg) != DialogResult.OK)
                return true;
            string fileName = dlg.FileName.Text;
            try
            {
                var arch = this.config.GetArchitecture(dlg.SelectedArchitectureName);
                if (arch is null)
                    return true;
                var platform = new DefaultPlatform(Services, arch);
                var asm = arch.CreateAssembler(null);
                await CloseProject();
                await SwitchInteractor(InitialPageInteractor);
                await InitialPageInteractor.Assemble(fileName, asm, platform);
                RememberFilenameInMru(fileName);
                if (fileName.EndsWith(Project_v5.FileExtension))
                {
                    ProjectFileName = fileName;
                }
            }
            catch (Exception e)
            {
                await uiSvc.ShowError(e, "An error occurred while assembling {0}.", fileName);
            }
            return true;
        }

        public async ValueTask<bool> OpenBinaryAs(string initialFilename)
        {
            using IOpenAsDialog? dlg = dlgFactory.CreateOpenAsDialog(initialFilename);
            if (await uiSvc.ShowModalDialog(dlg) != DialogResult.OK)
                    return false;
            
            string fileName = dlg.FileName.Text;
            try
            {
                LoadDetails details = dlg.GetLoadDetails();
                await CloseProject();
                await SwitchInteractor(InitialPageInteractor);
                await pageInitial.OpenBinaryAs(fileName, details);
                if (fileName.EndsWith(Project_v5.FileExtension))
                {
                    ProjectFileName = fileName;
                }
            }
            catch (Exception ex)
            {
               await uiSvc.ShowError(
                    ex,
                    string.Format("An error occurred when opening the file {0}.", dlg.FileName.Text));
            }
            finally
            {
                dlg?.Dispose();
            }
            return true;
        }

        public async ValueTask CloseProject()
        {
            if (IsDecompilerLoaded)
            {
                if (await uiSvc.Prompt("Do you want to save any changes made to the decompiler project?"))
                {
                    if (!await Save())
                        return;
                }
            }

            CloseAllDocumentWindows();
            sc.RequireService<IProjectBrowserService>().Clear();
            sc.RequireService<IProcedureListService>().Clear();
            sc.RequireService<IStackTraceService>().Clear();
            diagnosticsSvc.ClearDiagnostics();
            decompilerSvc.Decompiler = null;
            this.ProjectFileName = null;
        }

        private void CloseAllDocumentWindows()
        {
            foreach (var frame in uiSvc.DocumentWindows.ToArray())
            {
                frame.Close();
            }
        }

        public InitialPageInteractor InitialPageInteractor
        {
            get { return pageInitial; }
        }

        public IScannedPageInteractor ScannedPageInteractor
        {
            get { return pageScanned; }
        }

        public IAnalyzedPageInteractor AnalyzedPageInteractor
        {
            get { return pageAnalyzed; }
        }

        public IFinalPageInteractor FinalPageInteractor
        {
            get { return pageFinal; }
        }

        private static string MruListFile
        {
            get { return Path.Combine(SettingsDirectory, "mru.txt"); }
        }

        public async ValueTask RestartRecompilation()
        {
            if (!IsDecompilerLoaded)
                return;

            foreach (var program in decompilerSvc.Decompiler!.Project.Programs)
            {
                program.Reset();
            }
            await SwitchInteractor(this.InitialPageInteractor);
            
            CloseAllDocumentWindows();
            sc.RequireService<IStackTraceService>().Clear();
            diagnosticsSvc.ClearDiagnostics();
            projectBrowserSvc.Reload();
            projectBrowserSvc.Show();
        }

        public async ValueTask NextPhase()
        {
            try
            {
                IPhasePageInteractor? next = NextPage(CurrentPhase);

                if (next is { })
                {
                    await SwitchInteractor(next);
                }
            }
            catch (Exception ex)
            {
                await uiSvc.ShowError(ex, "Unable to proceed.");
            }
            workerDlgSvc.FinishBackgroundWork();
        }

        private IPhasePageInteractor? NextPage(IPhasePageInteractor phase)
        {
            IPhasePageInteractor? next = null;
            if (phase == pageInitial)
            {
                next = pageScanned;
            }
            else if (phase == pageScanned)
            {
                next = pageAnalyzed;
            }
            else if (phase == pageAnalyzed)
            {
                next = pageFinal;
            }
            return next;
        }

        public async ValueTask FinishDecompilation()
        {
            try
            {
                IPhasePageInteractor prev = CurrentPhase;
                await workerDlgSvc.StartBackgroundWork("Finishing decompilation.", delegate()
                {
                    for (;;)
                    {
                        var next = NextPage(prev);
                        if (next is null)
                            break;
                        next.PerformWork(workerDlgSvc);
                        prev = next;
                    }
                });
                prev.EnterPage();
                CurrentPhase = prev;
                projectBrowserSvc.Reload();
                procedureListSvc.Load(decompilerSvc.Decompiler!.Project!);
            }
            catch (Exception ex)
            {
                await uiSvc.ShowError(ex, "An error occurred while finishing decompilation.");
            }
            workerDlgSvc.FinishBackgroundWork();
        }

        public void LayoutMdi(DocumentWindowLayout layout)
        {
            form.LayoutMdi(layout);
        }

        public async ValueTask ShowAboutBox()
        {
            using (IAboutDialog dlg = dlgFactory.CreateAboutDialog())
            {
                await uiSvc.ShowModalDialog(dlg);
            }
        }

        public async ValueTask EditFind()
        {
            using (ISearchDialog dlg = dlgFactory.CreateSearchDialog())
            {
                if (await uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
                {
                    Func<int, Program, bool> filter = GetScannedFilter(dlg);
                    var re = Core.Dfa.Automaton.CreateFromPattern(dlg.Patterns.Text);
                    if (re == null)
                        return;
                    var hits = this.decompilerSvc.Decompiler!.Project!.Programs
                        .SelectMany(program => 
                            program.SegmentMap.Segments.Values.SelectMany(seg =>
                            {
                                return ReMatches(program, seg, filter, re);
                            }));
                    srSvc.ShowAddressSearchResults(hits, new CodeSearchDetails());
                }
            }
        }

        private static IEnumerable<AddressSearchHit> ReMatches(Program program, ImageSegment seg, Func<int, Program, bool> filter, Core.Dfa.Automaton re)
        {
            if (seg.MemoryArea is ByteMemoryArea mem)
            {
                //$REVIEW: only support byte granularity searches.
                var linBaseAddr = seg.MemoryArea.BaseAddress.ToLinear();
                return re.GetMatches(
                        mem.Bytes,
                        0,
                        (int) seg.MemoryArea.Length)
                    .Where(o => filter(o, program))
                    .Select(offset => new AddressSearchHit(
                        program,
                        program.SegmentMap.MapLinearAddressToAddress(
                            linBaseAddr + (ulong) offset),
                        0))
                    .OrderBy(f => f.Address)
                    .Distinct();
            }
            else
            {
                return Array.Empty<AddressSearchHit>();
            }
        }

        private Func<int, Program, bool> GetScannedFilter(ISearchDialog dlg)
        {
            if (dlg.ScannedMemory.Checked)
            {
                if (dlg.UnscannedMemory.Checked)
                    return (o, map) => true;
                else
                    return (o, program) =>
                    {
                        var addr = program.SegmentMap.MapLinearAddressToAddress(
                            (ulong)
                             ((long)program.SegmentMap.BaseAddress.ToLinear() + o));
                        return program.ImageMap.TryFindItem(addr, out var item)
                            && item.DataType != null &&
                            item.DataType is not UnknownType;
                    };
            }
            else if (dlg.UnscannedMemory.Checked)
            {
                return (o, program) =>
                    {
                        var addr = program.SegmentMap.MapLinearAddressToAddress(
                              (ulong)((long) program.SegmentMap.BaseAddress.ToLinear() + o));
                        return program.ImageMap.TryFindItem(addr, out var item)
                            && item.DataType is null ||
                            item.DataType is UnknownType;
                    };
            }
            else
                throw new NotSupportedException();
        }

        public void ViewProjectBrowser()
        {
            this.projectBrowserSvc.Show();
        }

        public void ViewSegmentList()
        {
            var segmentListSvc = sc.RequireService<ISegmentListService>();
            //$TODO: need a better "currently selected program" service
            var projectBrowser = sc.RequireService<IProjectBrowserService>();
            var program = projectBrowser.CurrentProgram;
            if (program is { })
            {
                segmentListSvc.ShowSegments(program);
            }
        }

        public void ViewProcedureList()
        {
            this.procedureListSvc.Show();
        }

        public void FindProcedures(ISearchResultService svc)
        {
            var hits = this.decompilerSvc.Decompiler!.Project.Programs
                .SelectMany(program => program.Procedures.Select(proc =>
                    new ProcedureSearchHit(program, proc.Key, proc.Value)))
                .ToList();
            svc.ShowSearchResults(new ProcedureSearchResult(this.sc, hits));
        }

        public async ValueTask FindStrings(ISearchResultService srSvc)
        {
            using (var dlgStrings = dlgFactory.CreateFindStringDialog())
            {
                if (await uiSvc.ShowModalDialog(dlgStrings) == DialogResult.OK)
                {
                    var criteria = dlgStrings.GetCriteria();
                    var hits = this.decompilerSvc.Decompiler!.Project.Programs
                        .SelectMany(p => new StringFinder(p).FindStrings(criteria));
                    srSvc.ShowAddressSearchResults(
                       hits,
                       new StringSearchDetails(criteria));
                }
            }
        }

        public void ViewDisassemblyWindow()
        {
            //$TODO: these need " current program"  to work.
            //var dasmService = sc.GetService<IDisassemblyViewService>();
            //dasmService.ShowWindow();
        }

        public void ViewMemoryWindow()
        {
            //var memService = sc.GetService<ILowLevelViewService>();
            ////$TODO: determine "current program".
            //memService.ViewImage(this.decompilerSvc.Decompiler.Project.Programs.First());
            //memService.ShowWindow();
        }

        public void ViewCallGraph()
        {
            var project = decompilerSvc.Decompiler!.Project;
            //$TODO: what about mutiple programs in project?
            if (project is null || project.Programs.Count != 1)
                return;

            var program = project.Programs[0];
            var cgvSvc = sc.RequireService<ICallGraphViewService>();
            var title = string.Format("{0} {1}", program.Name, Resources.CallGraphTitle);
            cgvSvc.ShowCallgraph(program, title);
        }

        public void ToolsHexDisassembler()
        {
            var hexDasmSvc = sc.RequireService<IHexDisassemblerService>();
            hexDasmSvc.Show();
        }

        public async ValueTask ToolsOptions()
        {
            var dlg = dlgFactory.CreateUserPreferencesDialog();
            if (await uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
            {
                var uiPrefsSvc = Services.RequireService<IUiPreferencesService>();
                uiPrefsSvc.WindowSize = form.Size;
                uiPrefsSvc.WindowState = form.WindowState;
                uiPrefsSvc.Save();
            }
        }

        public async ValueTask ToolsKeyBindings()
        {
            var dlg = dlgFactory.CreateKeyBindingsDialog(uiSvc.KeyBindings);
            if (await uiSvc.ShowModalDialog(dlg) == DialogResult.OK)
            {
                uiSvc.KeyBindings = dlg.KeyBindings; 
                //$TODO: save in user settings.
            }
        }

        /// <summary>
        /// Saves the project. 
        /// </summary>
        /// <returns>False if the user cancelled the save, true otherwise.</returns>
        public async ValueTask<bool> Save()
        {
            if (decompilerSvc.Decompiler == null)
                return true;
            if (string.IsNullOrEmpty(this.ProjectFileName))
            {
                var filename = decompilerSvc.Decompiler.Project.Programs[0].Location.FilesystemPath;
                string? newName = await uiSvc.ShowSaveFileDialog(
                    Path.ChangeExtension(filename, Project_v5.FileExtension));
                if (newName == null)
                    return false;
                RememberFilenameInMru(newName);
                ProjectFileName = newName;
            }

            var fsSvc = Services.RequireService<IFileSystemService>();
            var saver = new ProjectSaver(sc);
            var projectLocation = ImageLocation.FromUri(ProjectFileName);
            var sProject = saver.Serialize(projectLocation, decompilerSvc.Decompiler.Project);

            using (var xw = fsSvc.CreateXmlWriter(ProjectFileName))
            {
                saver.Save(sProject, xw);
            }
            return true;
        }

        private static string SettingsDirectory
        {
            get
            {
                if (dirSettings is null)
                {
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    dir = Path.Combine(dir, "reko");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    dirSettings = dir;
                }
                return dirSettings;
            }
        }

        public async ValueTask SwitchInteractor(IPhasePageInteractor interactor)
        {
            if (interactor == CurrentPhase)
                return;

            if (CurrentPhase != null)
            {
                if (!CurrentPhase.LeavePage())
                    return;
            }
            CurrentPhase = interactor;
            await workerDlgSvc.StartBackgroundWork("Entering next phase...", delegate()
            {
                interactor.PerformWork(workerDlgSvc);
            });
            interactor.EnterPage();
            form.UpdateToolbarState();
        }

        public void UpdateWindowTitle()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(decompilerSvc.ProjectName))
            {
                sb.Append(decompilerSvc.ProjectName);
                //$REFACTOR: dirtiness of project is not limited to first page.
                //if (MainForm.InitialPage.IsDirty)
                //    sb.Append('*');
                sb.Append(" - ");
            }
            sb.AppendFormat(
                "Reko Decompiler ({0})", 
                Environment.Is64BitProcess ? "64-bit" : "32-bit");
            this.TitleText = sb.ToString();
        }

        #region ICommandTarget members

        /// <summary>
        /// Determines a command target that should be handling commands. This 
        /// is in essence the "router" that routes commands.
        /// </summary>
        /// <returns></returns>
        private ICommandTarget GetSubCommandTarget()
        {
            if (searchResultsTabControl.ContainsFocus)
                return searchResultsTabControl;
            if (projectBrowserSvc.ContainsFocus)
                return projectBrowserSvc;
            if (procedureListSvc.ContainsFocus)
                return procedureListSvc;
            return subWindowCommandTarget;
        }

        public bool QueryStatus(CommandID cmdId, CommandStatus cmdStatus, CommandText cmdText)
        {
            var ct = GetSubCommandTarget();
            if (ct != null && ct.QueryStatus(cmdId, cmdStatus, cmdText))
                return true;
            
            if (currentPhase != null && currentPhase.QueryStatus(cmdId, cmdStatus, cmdText))
                return true;
            if (cmdId.Guid == CmdSets.GuidReko)
            {
                if (QueryMruItem(cmdId.ID, cmdStatus, cmdText))
                    return true;

                switch ((CmdIds) cmdId.ID)
                {
                case CmdIds.FileOpen:
                case CmdIds.FileExit:
                case CmdIds.FileOpenAs:
                case CmdIds.FileAssemble:
                case CmdIds.ViewProjectBrowser:
                case CmdIds.ViewProcedureList:
                case CmdIds.ToolsHexDisassembler:
                case CmdIds.ToolsOptions:
                case CmdIds.WindowsCascade: 
                case CmdIds.WindowsTileVertical:
                case CmdIds.WindowsTileHorizontal:
                case CmdIds.WindowsCloseAll: 
                case CmdIds.HelpAbout: 
                    cmdStatus.Status = MenuStatus.Enabled | MenuStatus.Visible;
                    return true;
                    //$TODO: finish implementing this :)
                case CmdIds.ToolsKeyBindings:
                    cmdStatus.Status = MenuStatus.Visible;
                    return true;
                case CmdIds.FileMru:
                    cmdStatus.Status = MenuStatus.Visible;
                    return true;
                case CmdIds.ActionRestartDecompilation:
                    cmdStatus.Status = MenuStatus.Enabled | MenuStatus.Visible;
                    break;
                case CmdIds.ActionNextPhase:
                    cmdStatus.Status = currentPhase!.CanAdvance
                        ? MenuStatus.Enabled | MenuStatus.Visible
                        : MenuStatus.Visible;
                    return true;
                case CmdIds.FileNewScript:
                case CmdIds.FileAddBinary:
                case CmdIds.FileAddMetadata:
                case CmdIds.FileAddScript:
                case CmdIds.FileSave:
                case CmdIds.FileCloseProject:
                case CmdIds.EditFind:
                case CmdIds.ViewCallGraph:
                case CmdIds.ViewFindAllProcedures:
                case CmdIds.ViewFindStrings:
                case CmdIds.ViewSegmentList:
                    cmdStatus.Status = IsDecompilerLoaded
                        ? MenuStatus.Enabled | MenuStatus.Visible
                        : MenuStatus.Visible;
                    return true;
                }
            }
            return false;
        }

        private bool QueryMruItem(int cmdId, CommandStatus cmdStatus, CommandText cmdText)
        {
            int iMru = cmdId - (int)CmdIds.FileMru;
            if (0 <= iMru && iMru < mru.Items.Count)
            {
                cmdStatus.Status = MenuStatus.Visible | MenuStatus.Enabled;
                cmdText.Text = string.Format("{0}{1} {2}", cmdText.MnemonicPrefix, iMru + 1, mru.Items[iMru]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Dispatches menu commands.
        /// </summary>
        /// <param name="cmdSet"></param>
        /// <param name="cmdId"></param>
        /// <returns></returns>
        public async ValueTask<bool> ExecuteAsync(CommandID cmdId)
        {
            var ct = GetSubCommandTarget();
            if (ct != null && await ct.ExecuteAsync(cmdId))
            {
                form.UpdateToolbarState();
                return true;
            }
            if (currentPhase != null && await currentPhase.ExecuteAsync(cmdId))
            {
                form.UpdateToolbarState();
                return true;
            }
            if (cmdId.Guid == CmdSets.GuidReko)
            {
                if (await ExecuteMruFile(cmdId.ID))
                {
                    form.UpdateToolbarState();
                    return true;
                }

                bool retval = false;
                switch ((CmdIds)cmdId.ID)
                {
                case CmdIds.FileOpen: await OpenBinaryWithPrompt(); retval = true; break;
                case CmdIds.FileOpenAs: retval = await OpenBinaryAs(""); break;
                case CmdIds.FileAssemble: retval = await AssembleFile(); break;
                case CmdIds.FileSave: await Save(); retval = true; break;
                case CmdIds.FileAddMetadata: await AddMetadataFile(); retval = true; break;
                case CmdIds.FileNewScript: await CreateScriptFile(); retval = true; break;
                case CmdIds.FileAddScript: await AddScriptFile(); retval = true; break;
                case CmdIds.FileCloseProject: await CloseProject(); retval = true; break;
                case CmdIds.FileExit: form.Close(); retval = true; break;

                case CmdIds.ActionRestartDecompilation: await RestartRecompilation(); retval = true; break;
                case CmdIds.ActionNextPhase: await NextPhase(); retval = true; break;
                case CmdIds.ActionFinishDecompilation: await FinishDecompilation(); retval = true; break;

                case CmdIds.EditFind: await EditFind(); retval = true; break;

                case CmdIds.ViewProjectBrowser: ViewProjectBrowser(); retval = true; break;
                case CmdIds.ViewProcedureList: ViewProcedureList(); retval = true; break;
                case CmdIds.ViewDisassembly: ViewDisassemblyWindow(); retval = true; break;
                case CmdIds.ViewMemory: ViewMemoryWindow(); retval = true; break;
                case CmdIds.ViewCallGraph: ViewCallGraph(); retval = true; break;
                case CmdIds.ViewFindAllProcedures: FindProcedures(srSvc); retval = true; break;
                case CmdIds.ViewFindStrings: await FindStrings(srSvc); retval = true; break;
                case CmdIds.ViewSegmentList: ViewSegmentList(); retval = true; break;

                case CmdIds.ToolsHexDisassembler: ToolsHexDisassembler(); retval = true; break;
                case CmdIds.ToolsOptions: await ToolsOptions(); retval = true; break;
                case CmdIds.ToolsKeyBindings: await ToolsKeyBindings(); retval = true; break;

                case CmdIds.WindowsCascade: LayoutMdi(DocumentWindowLayout.None); retval = true; break;
                case CmdIds.WindowsTileVertical: LayoutMdi(DocumentWindowLayout.TiledVertical); retval = true; break;
                case CmdIds.WindowsTileHorizontal: LayoutMdi(DocumentWindowLayout.TiledHorizontal); retval = true; break;
                case CmdIds.WindowsCloseAll: CloseAllDocumentWindows(); retval = true; break;

                case CmdIds.HelpAbout: await ShowAboutBox(); retval = true; break;
                }
                form.UpdateToolbarState();
                return retval;
            }
            return false;
        }

        private async ValueTask<bool> ExecuteMruFile(int cmdId)
        {
            int iMru = cmdId - (int)CmdIds.FileMru;
            if (0 <= iMru && iMru < mru.Items.Count)
            {
                string file = mru.Items[iMru];

                await CloseProject();
                await SwitchInteractor(InitialPageInteractor);
                try
                {
                    if (await pageInitial.OpenBinary(file))
                    {
                        RememberFilenameInMru(file);
                        if (file.EndsWith(Project_v5.FileExtension))
                        {
                            ProjectFileName = file;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await uiSvc.ShowError(
                        ex,
                        $"An error occurred when opening the file {file}.");
                }
                return true;
            }
            return false;
        }

        private bool IsDecompilerLoaded
        {
            get
            {
                if (decompilerSvc.Decompiler == null)
                    return false;
                return decompilerSvc.Decompiler.Project != null;
            }
        }
        #endregion

        // Event handlers //////////////////////////////

        private void miFileExit_Click(object sender, System.EventArgs e)
        {
            form.Close();
        }

        private void MainForm_Closed(object? sender, System.EventArgs e)
        {
            var uiPrefsSvc = sc.RequireService<IUiPreferencesService>();
            // It's OK if we can't save settings, just discard them.
            try
            {
                uiPrefsSvc.WindowSize = form.Size;
                uiPrefsSvc.WindowState = form.WindowState;
                uiPrefsSvc.Save();
            }
            catch { }
        }

        private void InitialPage_IsDirtyChanged(object sender, EventArgs e)
        {
            UpdateWindowTitle();
        }
    }
}
