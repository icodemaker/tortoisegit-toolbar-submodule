using System.Diagnostics;
using System.Windows.Forms;
using EnvDTE80;
using TortoiseGitToolbar.Config.Constants;
using Process = System.Diagnostics.Process;

namespace TortoiseGitToolbar.Services
{
    public interface ITortoiseGitLauncherService
    {
        void ExecuteTortoiseProc(ToolbarCommand command);
    }

    public class TortoiseGitLauncherService : ITortoiseGitLauncherService
    {
        private readonly IProcessManagerService _processManagerService;
        private readonly Solution2 _solution;

        public TortoiseGitLauncherService(IProcessManagerService processManagerService, Solution2 solution)
        {
            _processManagerService = processManagerService;
            _solution = solution;
        }

        public void ExecuteTortoiseProc(ToolbarCommand command)
        {
            var solutionPath = PathConfiguration.GetSolutionPath(_solution);
            var openedFilePath = PathConfiguration.GetOpenedFilePath(_solution);
            // todo: make the bash/tortoise paths configurable
            // todo: detect if the solution is a git solution first
            if (command == ToolbarCommand.Bash && PathConfiguration.GetGitBashPath() == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Could_not_find_Git_Bash_in_the_standard_install_path_,
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Git_Bash_not_found,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }
            if (command != ToolbarCommand.Bash && solutionPath == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_SolutionPath_You_need_to_open_a_solution_first,
                    Resources.Resources.TortoiseGitLauncherService_SolutionPath_No_solution_found,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }
            if (command != ToolbarCommand.Bash && PathConfiguration.GetTortoiseGitPath() == null)
            {
                MessageBox.Show(
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_Could_not_find_TortoiseGit_in_the_standard_install_path_,
                    Resources.Resources.TortoiseGitLauncherService_ExecuteTortoiseProc_TortoiseGit_not_found,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation
                );
                return;
            }

            ProcessStartInfo process;
            switch (command)
            {
                case ToolbarCommand.Bash:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetGitBashPath(),
                        "--login -i",
                        solutionPath
                    );
                    break;
                case ToolbarCommand.RebaseContinue:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetGitBashPath(),
                        @"--login -i -c 'echo; echo ""Running git rebase --continue""; echo; git rebase --continue; echo; echo ""Please review the output above and press enter to continue.""; read'",
                        solutionPath
                    );
                    break;
                case ToolbarCommand.FileLog:
                case ToolbarCommand.FileDiff:
                    var commandParam = command.ToString().Replace("File", string.Empty).ToLower();
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:{commandParam} /path:""{openedFilePath}"""
                    );
                    break;
                case ToolbarCommand.FileBlame:
                    var line = GetCurrentLine(_solution);
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:blame /path:""{openedFilePath}"" /line:{line}"
                    );
                    break;
                case ToolbarCommand.StashList:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:reflog /path:""{solutionPath}"" /ref:""refs/stash"""
                    );
                    break;
                //todo:修改Pull操作
                case ToolbarCommand.Pull:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:pull --progress -v --no-rebase ""origin"" /path:""{solutionPath}""" 
                    );
                    break;
                case ToolbarCommand.SubModuleAdd:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:subadd /path:""{solutionPath}"""
                    );
                    break;
                case ToolbarCommand.SubModuleUpdate:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:subupdate --recursive --remote /bkpath:""{solutionPath}"" /path:""{solutionPath}""" 
                    );
                    break;
                default:
                    process = _processManagerService.GetProcess(
                        PathConfiguration.GetTortoiseGitPath(),
                        $@"/command:{command.ToString().ToLower()} /path:""{solutionPath}"""
                    );
                    break;
            }

            if (process != null)
                Process.Start(process);
        }

        private static int GetCurrentLine(Solution2 solution)
        {
            if (solution.DTE?.ActiveDocument == null ||
                solution.DTE?.ActiveDocument?.Selection == null) return 0;
            var selection = solution.DTE?.ActiveDocument?.Selection;
            return selection?.CurrentLine;
        }
    }
}
