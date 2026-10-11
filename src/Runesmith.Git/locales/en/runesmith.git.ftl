### The Git plugin's text in English, the source every translation starts from. The build turns each message into a member of the
### Strings class, such as Strings.CommandCommit.
###
### In these messages $branch, $upstream, $remote and $destination are names from the repository, such as main, origin/main and
### origin; $commit is the short id of a commit, such as 1a2b3c4d. They are never translated.

## What the commands tell in notifications, and the names of their background tasks.

init-done = Created a Git repository
# $folder is the name of the open folder.
init-done-message = { $folder } is a Git repository now; its files show in the Commit window.
init-failed = Could not create a Git repository
# The button of a notification that opens the Commit window.
show-changes = Show Changes

no-remote = No remote
no-remote-fetch = The repository has no remote to fetch from. Add one with git remote add, such as origin with the address of the repository on its server.
no-remote-pull = The repository has no remote to pull from. Add one with git remote add, such as origin with the address of the repository on its server.
no-remote-push = The repository has no remote to push to. Add one with git remote add, such as origin with the address of the repository on its server.

task-fetching = Fetching
# Variables:
#   $count (Number) - how many commits the upstream has that the branch does not
fetch-behind =
    { $count ->
        [one] 1 commit to pull
       *[other] { $count } commits to pull
    }
# Variables:
#   $count (Number) - how many commits the upstream has that the branch does not
fetch-behind-message =
    { $count ->
        [one] { $upstream } has 1 commit that { $branch } does not have yet.
       *[other] { $upstream } has { $count } commits that { $branch } does not have yet.
    }
# The button that pulls.
update-project = Update Project
fetch-done = Fetched
fetch-done-up-to-date = { $branch } is up to date with { $upstream }.
fetch-done-all = Every remote is fetched.

head-detached = HEAD is detached
head-detached-pull = Check out a branch to update it.
head-detached-push = Check out a branch, or create one here, to push.
no-upstream = { $branch } has no upstream branch
no-upstream-message = Push it to set one, or track a remote branch from the branch widget.
task-updating = Updating { $branch }
pull-done = Updated { $branch }
# Variables:
#   $count (Number) - how many commits came in
pull-done-message =
    { $count ->
        [one] 1 commit from { $upstream }.
       *[other] { $count } commits from { $upstream }.
    }
pull-up-to-date = { $branch } is up to date
pull-up-to-date-message = Nothing new on { $upstream }.

force-push-title = Force push?
force-push-message = This replaces { $destination } with { $branch }. Commits there that { $branch } does not have are lost, unless the remote moved since your last fetch, in which case the push stops.
force-push = Force Push
task-pushing = Pushing { $branch } to { $destination }
# Variables:
#   $count (Number) - how many commits were pushed
push-done-commits =
    { $count ->
        [one] Pushed 1 commit to { $destination }
       *[other] Pushed { $count } commits to { $destination }
    }
push-done = Pushed { $branch } to { $destination }
push-tracks = { $branch } tracks { $destination } from now on.
push-rejected = Push rejected
push-rejected-message = { $destination } has commits that { $branch } does not have. Update the project to bring them in, then push again.
update-and-push = Update and Push
force-push-stopped = Force push stopped
force-push-stopped-message = { $destination } moved since the last fetch, so forcing could lose someone's commits. Fetch, look at what changed, and try again.
force-push-amended-title = Force push the amended commit?
force-push-amended-message = The commit you amended was pushed already, so pushing it again replaces it on the remote.

commit-amended = Amended the last commit
# Variables:
#   $count (Number) - how many files were committed
commit-done =
    { $count ->
        [one] Committed 1 file
       *[other] Committed { $count } files
    }

## Branches.

# The title of the dialog that asks for a new branch's name.
new-branch = New Branch
# $start is the branch or commit the new branch starts from.
new-branch-from = From { $start }
name-label = Name
create = Create
new-branch-checkout = Check out the new branch
create-branch-failed = Could not create { $branch }
rename-branch-title = Rename { $branch }
rename-branch-placeholder = New name
rename-same = Type a different name.
rename-branch-failed = Could not rename { $branch }
branch-name-empty = Type a name.
# $name is what the user typed.
branch-exists = A branch named { $name } exists already.
branch-name-invalid = { $name } is not a valid branch name; names cannot contain spaces, ~, ^, :, ?, * or [, or two dots in a row.

delete-branch-title = Delete { $branch }?
delete-remote-branch-message = This deletes the branch { $branch } on { $remote } for everyone who uses it.
delete-local-branch-message = This deletes the local branch { $branch }; its commits stay where other branches have them.
delete = Delete
task-deleting = Deleting { $branch }
delete-branch-done = Deleted { $branch }
delete-branch-pointed = It pointed at { $commit }.
# The button that brings a deleted branch back.
restore = Restore
restore-branch-failed = Could not restore { $branch }
delete-checked-out = { $branch } is checked out
delete-checked-out-message = Check out another branch before deleting this one.
delete-unmerged = { $branch } is not fully merged
delete-unmerged-message = Some of its commits are on no other branch, and are lost when it is deleted. Delete it anyway?
delete-anyway = Delete Anyway
delete-branch-failed = Could not delete { $branch }

track-failed = Could not track { $branch }
track-done = { $branch } tracks { $upstream }

# $target is the branch that is merged into.
task-merging = Merging { $branch } into { $target }
merge-done = Merged { $branch } into { $target }
# The title of the history that compares two branches. $current is the checked out branch.
compare-title = Commits { $current } and { $branch } do not share

## The list that the Check Out command shows.

checkout-title = Check out
# $branch is the branch that is checked out now.
checkout-title-on = Check out (now on { $branch })
checkout-placeholder = A branch or a tag
checkout-empty = No branch or tag has that name
checkout-local = Local branches
checkout-remote = Remote branches
checkout-tags = Tags

# $target is the branch, tag or commit that is checked out.
task-checking-out = Checking out { $target }
checkout-done = Checked out { $target }
checkout-done-with-changes = Your local changes came along.
checkout-overwrite = Local changes would be overwritten
# Shown under Git's own message about the files in the way.
checkout-overwrite-message = Smart Checkout stashes your changes, checks out { $target } and brings them back. Force Checkout throws away the changes that are in the way.
force-checkout = Force Checkout
smart-checkout = Smart Checkout
checkout-conflicts = Checked out { $target }, with conflicts
checkout-conflicts-message = Your local changes conflict with { $target }; resolve the conflicts. The changes stay in the stash until you drop it.
stash-local-failed = Could not stash the local changes
unstash-local-failed = Could not bring back the local changes

## Commits: cherry-pick, revert and reset.

task-cherry-picking = Cherry-picking { $commit }
cherry-pick-done = Cherry-picked { $commit }
task-reverting = Reverting { $commit }
revert-done = Reverted { $commit }
reset-title = Reset { $branch } to { $commit }
reset-message = Soft keeps the changes after it staged. Mixed keeps them in the working tree, not staged. Hard throws them and every local change away.
# The three ways to reset, as Git names them.
reset-hard = Hard
reset-soft = Soft
reset-mixed = Mixed
reset-hard-title = Hard reset { $branch }?
reset-hard-message = Every local change and the commits after { $commit } on { $branch } are thrown away.
reset = Reset
reset-failed = Could not reset { $branch }

## The stash.

stash-nothing = Nothing to stash
stash-nothing-message = There are no local changes.
stash-title = Stash Changes
stash-prompt = Put every local change, untracked files included, aside
stash-placeholder = Description (optional)
stash-failed = Could not stash
stash-done = Stashed the local changes
unstash = Unstash
stash-empty = The stash is empty
unstash-prompt = Which changes should come back?
task-unstashing = Unstashing
unstash-done = Brought back the stashed changes

## A merge, rebase, cherry-pick or revert that stopped at conflicts. $operation is merge, rebase, cherry-pick or revert.

abort-title =
    { $operation ->
        [merge] Abort the merge?
        [rebase] Abort the rebase?
        [cherry-pick] Abort the cherry-pick?
       *[revert] Abort the revert?
    }
abort-message = Everything goes back to how it was before it started; conflict resolutions are lost.
abort = Abort
abort-failed = Could not abort
task-continuing =
    { $operation ->
        [merge] Continuing the merge
        [rebase] Continuing the rebase
        [cherry-pick] Continuing the cherry-pick
       *[revert] Continuing the revert
    }
# Variables:
#   $count (Number) - how many files have conflicts; 0 when Git did not say
conflicts =
    { $count ->
        [0] Conflicts to resolve
        [one] 1 file has conflicts
       *[other] { $count } files have conflicts
    }
conflicts-message = Resolve them in the files, stage them, and commit; or abort from the Commit window.

## Comparing and rolling back files. $file is a file's name.

diff-head-and-staged = { $file } (HEAD and staged)
diff-staged-and-working = { $file } (staged and working tree)
diff-head-and-working = { $file } (HEAD and working tree)
# The names of the two sides of a comparison.
diff-staged = Staged
diff-working-tree = Working tree
# The side of a comparison for a file that did not exist yet.
diff-empty = Empty
no-changes = { $file } has no changes

rollback-title-one = Roll back { $file }?
# Variables:
#   $count (Number) - how many files, more than one
rollback-title-many =
    { $count ->
        [one] Roll back 1 file?
       *[other] Roll back { $count } files?
    }
# The first files and how many more there are. $names is a list of file names.
# Variables:
#   $count (Number) - how many files are not named
rollback-names-more = { $names } and { $count } more
# Variables:
#   $files (Number) - how many files are rolled back
rollback-message =
    { $files ->
        [one] { $names } goes back to how it is in the last commit; its changes are lost.
       *[other] { $names } go back to how they are in the last commit; their changes are lost.
    }
# Added to rollback-message when some of the files are untracked.
rollback-untracked = Untracked files are deleted.
roll-back = Roll Back
rollback-failed = Could not roll back
rollback-done-one = Rolled back { $file }
# Variables:
#   $count (Number) - how many files, more than one
rollback-done-many =
    { $count ->
        [one] Rolled back 1 file
       *[other] Rolled back { $count } files
    }
# The title of the history of one file or folder. $path is its path in the repository.
history-of = History of { $path }

## A task that stopped. $task is the name of the task, such as Fetching.

task-cancelled = { $task }: cancelled
task-failed = { $task } failed
credentials-rejected = The remote did not accept your credentials.
show-git-output = Show Git Output

## Counts and times.

# Variables:
#   $count (Number) - how many commits
commits =
    { $count ->
        [one] 1 commit
       *[other] { $count } commits
    }
time-just-now = just now
# Variables:
#   $count (Number) - how many minutes
time-minutes-ago =
    { $count ->
        [one] 1 minute ago
       *[other] { $count } minutes ago
    }
# Variables:
#   $count (Number) - how many hours
time-hours-ago =
    { $count ->
        [one] 1 hour ago
       *[other] { $count } hours ago
    }
# Variables:
#   $count (Number) - how many days
time-days-ago =
    { $count ->
        [one] 1 day ago
       *[other] { $count } days ago
    }

## The commands, in the Git menu, the context menus and the command palette.

command-commit = Commit...
command-commit-description = Show the Commit window with the message box focused.
command-push = Push...
command-push-description = Push the current branch, showing the commits that go and where.
command-pull = Pull...
command-pull-description = Update the project: pull the current branch's upstream, keeping local changes.
command-fetch = Fetch
command-fetch-description = Fetch every remote and drop remote branches deleted there.
command-new-branch = New Branch...
command-new-branch-description = Create a branch at HEAD, and check it out.
command-branches = Branches...
command-branches-description = Open the branch popup: check out, create, merge, rename and delete branches.
command-checkout = Check Out...
command-checkout-description = Check out a branch or a tag, picked from a list.
command-show-history = Show History
command-show-history-description = Show the Git window with the log and its graph.
command-file-history = Show File History
command-file-history-description = Show the commits that changed the current file, following renames.
command-show-diff = Show Diff
command-show-diff-description = Compare the current file with its last committed version.
command-rollback-file = Roll Back...
command-rollback-file-description = Undo every change to the current file, or to the files of a folder, since the last commit.
command-add-to-gitignore = Add to .gitignore
command-add-to-gitignore-description = Have Git ignore an untracked file or folder, in the repository's .gitignore.
command-stash = Stash Changes...
command-stash-description = Put every local change aside, untracked files included.
command-unstash = Unstash Changes...
command-unstash-description = Bring back changes put aside with Stash Changes.
command-initialize = Initialize Repository
command-initialize-description = Make the open folder a Git repository.
command-clone = Clone Repository...
command-clone-description = Clone a repository from your accounts on hosting services, or from any Git URL, and open it
command-create-pull-request = Create Pull Request...
command-create-pull-request-description = Propose the current branch's changes in a pull request on the repository's hosting service
# In the next six messages $host is the name of a hosting service, such as GitHub.
command-open-on = Open on { $host }
command-open-on-description = Open the current file, at the selected lines, on { $host }
command-copy-link = Copy { $host } Link
command-copy-link-description = Copy the link to the current file and its selected lines on { $host }
command-open-repository = Open Repository on { $host }
command-open-repository-description = Open the repository's page on { $host }
clone-needs-dialogs = Cloning needs a newer Runesmith
clone-needs-dialogs-message = This version of Runesmith does not let plugins show dialogs.
link-copied = Link copied
cancel = Cancel

## Settings.

setting-pull-strategy = Pull strategy
setting-pull-strategy-description = How Update Project brings in the upstream's commits: as your Git configuration's pull.rebase says (merging when it says nothing), by rebasing your commits on them, or with a merge.
setting-pull-strategy-git-config = As Git is configured
setting-pull-strategy-rebase = Rebase
setting-pull-strategy-merge = Merge
setting-auto-fetch = Fetch automatically every (minutes)
setting-auto-fetch-description = Fetches every remote in the background, so the branch widget shows what there is to pull; 0 turns it off.
setting-subject-length = Commit subject length hint
setting-subject-length-description = The commit message box shows the subject's length once it is longer than this.

## The branch widget in the toolbar and the branch and sync items of the status bar: their tooltips.

branch-detached-at = HEAD is detached at { $commit }
branch-name = Branch { $branch }
branch-not-tracking = Not tracking a remote branch; pushing sets one
branch-up-to-date = Up to date with { $upstream }
branch-tracking = Tracking { $upstream }
# Variables:
#   $count (Number) - how many commits the branch has that its upstream does not
commits-to-push =
    { $count ->
        [one] 1 commit to push
       *[other] { $count } commits to push
    }
status-click-branches = Click for branches
status-click-fetch = Click to fetch
sync-syncing = Syncing with the remote...
sync-no-upstream = No upstream branch. Click to fetch every remote
sync-up-to-date = Up to date with { $upstream }. Click to fetch

## The branch popup.

branches-search = Search branches and actions
popup-update-project = Update Project...
# The headers of the popup's groups of branches.
group-recent = Recent
group-local = Local
group-remote = Remote
group-tags = Tags
# Shown next to a branch whose upstream was deleted on the remote.
branch-gone = gone
branch-tip-tracking = { $branch }, tracking { $upstream }
branch-tip-tracking-gone = { $branch }, tracking { $upstream } (deleted on the remote)
branch-tip-not-tracking = { $branch }, not tracking a remote branch
branch-tip-remote = { $branch } on { $remote }
# $name is the tag's name.
branch-tip-tag = Tag { $name }
# The menu of one branch.
menu-checkout = Checkout
menu-new-branch-from = New Branch from { $branch }...
menu-compare-with = Compare with { $branch }
menu-merge-into = Merge into { $branch }
menu-rename = Rename...
menu-track = Track
menu-delete-on = Delete on { $remote }
push = Push
history-of-branch = History of { $branch }

## The Push dialog.

push-dialog-title = Push Commits
push-to-tracked = To { $destination }, which { $branch } tracks
push-sets-upstream = { $branch } has no upstream yet; this push sets it
push-empty-title = Nothing to push
push-empty-hint = { $destination } has every commit of { $branch }.
push-new-branch-title = A new branch
push-new-branch-hint = { $branch } is new on { $remote }; pushing it creates it there and tracks it.
push-force-option = Force push, if the remote is where it was at the last fetch
# Variables:
#   $count (Number) - how many commits the remote branch has that the local one does not
push-behind-hint =
    { $count ->
        [one] { $destination } has 1 commit that { $branch } does not have. Update the project first, or the push is rejected.
       *[other] { $destination } has { $count } commits that { $branch } does not have. Update the project first, or the push is rejected.
    }
push-many-commits = 100 or more commits
# The tooltip of the button that closes a message.
dismiss = Dismiss
# Variables:
#   $count (Number) - how many months
time-months-ago =
    { $count ->
        [one] 1 month ago
       *[other] { $count } months ago
    }
# Variables:
#   $count (Number) - how many years
time-years-ago =
    { $count ->
        [one] 1 year ago
       *[other] { $count } years ago
    }

## The Commit window.

window-commit = Commit
commit-no-changes = No changes
commit-no-changes-hint = The working tree matches the last commit. Edit files and they show here, ready to commit.
commit-message-placeholder = Commit message
# The check box that replaces the last commit.
commit-amend = Amend
commit-amend-tip = Replace the last commit with the staged changes and this message
commit-button = Commit
commit-amend-button = Amend Commit
commit-tip = Commit the staged changes (Ctrl+Enter)
commit-and-push = Commit and Push
commit-and-push-tip = Commit, then push to the upstream branch (Ctrl+Shift+Enter)
commit-recent-messages = Recent messages
commit-options = Commit options
commit-sign-off = Sign Off
commit-sign-off-tip = Add a Signed-off-by line with your name and address
commit-skip-hooks = Skip Hooks
commit-skip-hooks-tip = Do not run the pre-commit and commit-msg hooks
# Variables:
#   $total (Number) - how many files changed
commit-summary-none-staged =
    { $total ->
        [one] 1 file changed, none staged
       *[other] { $total } files changed, none staged
    }
# Variables:
#   $total (Number) - how many files changed
#   $staged (Number) - how many of them are staged
commit-summary-staged =
    { $total ->
        [one] { $staged } of 1 file staged
       *[other] { $staged } of { $total } files staged
    }
# Shown when the first line of the message is long.
# Variables:
#   $length (Number) - how many characters the first line has
#   $limit (Number) - how many it should have at most
commit-subject-length = Subject { $length } / { $limit }
# Variables:
#   $limit (Number) - how many characters a subject should have at most
commit-subject-length-tip = Subjects of { $limit } characters or fewer read best in logs; move the rest to the body, after an empty line.
commit-nothing-staged = Nothing is staged
commit-nothing-staged-message = Check the files to commit, or stage every change and commit them all.
# Variables:
#   $count (Number) - how many files would be staged
commit-stage-all =
    { $count ->
        [one] Stage All (1 file) and Commit
       *[other] Stage All ({ $count } files) and Commit
    }
commit-nothing = Nothing to commit
commit-conflicts-first = Resolve the conflicts first
commit-hook-stopped = A hook stopped the commit
commit-failed = The commit failed
commit-without-hooks = Commit Without Hooks
stage = Stage
unstage = Unstage
stage-failed = Could not stage
unstage-failed = Could not unstage
loading = Loading...
no-commits-yet = No commits yet

# The groups of the list of changed files. Staged is diff-staged.
group-conflicts = Conflicts
group-changes = Changes
group-untracked = Untracked Files
# What happened to a file, as the tooltip of its letter says.
change-added = Added
change-untracked = Untracked
change-deleted = Deleted
change-renamed = Renamed
change-copied = Copied
change-type-changed = Type changed
change-conflict = Conflict
change-modified = Modified
# Shown after a renamed file's name. $path is the file's name or path before.
change-renamed-from-short = from { $path }
change-renamed-from = Renamed from { $path }
# The menu of the changed files.
menu-open = Open
menu-reveal-finder = Reveal in Finder
menu-reveal-file-manager = Reveal in File Manager
# Variables:
#   $count (Number) - how many files are selected
menu-copy-path =
    { $count ->
        [one] Copy Path
       *[other] Copy Paths
    }
# Variables:
#   $count (Number) - how many files are selected
menu-copy-relative-path =
    { $count ->
        [one] Copy Relative Path
       *[other] Copy Relative Paths
    }

# The banner over the changes while a merge, rebase, cherry-pick or revert waits for conflicts to be resolved.
state-merging = Merging
# Variables:
#   $count (Number) - how many files have conflicts
state-merge-conflicts =
    { $count ->
        [one] Resolve the conflicts in 1 file, stage it, then commit to finish the merge.
       *[other] Resolve the conflicts in { $count } files, stage them, then commit to finish the merge.
    }
state-merge-resolved = The conflicts are resolved. Commit to finish the merge.
state-abort-merge = Abort Merge
state-cherry-picking = Cherry-picking
state-rebasing = Rebasing
state-reverting = Reverting
# Variables:
#   $count (Number) - how many files have conflicts
state-conflicts =
    { $count ->
        [one] Resolve the conflicts in 1 file and stage it, then continue.
       *[other] Resolve the conflicts in { $count } files and stage them, then continue.
    }
state-resolved = The conflicts are resolved. Continue to go on.
state-continue = Continue

## The Git window: the log.

log-search = Search messages
log-path = Path
log-branch-filter-tip = Show the commits of one branch
log-author-filter-tip = Show the commits of one author
log-path-filter-tip = Show the commits that changed a file or folder; press Enter to apply
log-clear-scope-tip = Show every branch again
log-refresh-tip = Read the log again
log-fetch-tip = Fetch every remote
log-no-repository = No Git repository
log-no-repository-hint = Open a folder in a Git repository to see its history.
log-failed = Could not read the log
log-no-match = No commits match
log-no-match-hint = Change or clear the filters to see more commits.
log-empty-hint = Commit your first changes from the Commit window.
# The names of the two filters, shown before their values, such as "Branch: main".
log-filter-branch = Branch:
log-filter-author = Author:
# The value of the branch filter while the log shows what a command asked for, and when it shows every branch.
log-filter-filtered = Filtered
log-filter-all = All
# The value of the author filter when it shows everyone's commits.
log-filter-anyone = Anyone
log-all-branches = All branches
log-current-branch = Current branch (HEAD)
log-loading-branches = Loading branches...
# The menu of one commit.
menu-checkout-branch = Checkout { $branch }
menu-checkout-revision = Checkout Revision
menu-cherry-pick = Cherry-pick
menu-revert-commit = Revert Commit
menu-reset-to-here = Reset { $branch } to Here
menu-reset-soft = Soft: keep the changes staged
menu-reset-mixed = Mixed: keep the changes in the working tree
menu-reset-hard = Hard: throw the changes away
menu-copy-hash = Copy Hash
# The tooltips of the labels next to a commit.
label-detached-head = HEAD (detached)
label-current-branch = { $branch }, the current branch
label-remote-branch = Remote branch { $branch }

## The Git window: the details of the selected commit.

details-no-commit = No commit selected
details-no-commit-hint = Select a commit to see its message and the files it changed.
details-failed = Could not read the commit
# Variables:
#   $count (Number) - how many files the commit changed
details-changed-files =
    { $count ->
        [one] 1 changed file
       *[other] { $count } changed files
    }
details-author = Author
details-committer = Committer
details-commit = Commit
# Variables:
#   $count (Number) - how many parents the commit has
details-parents =
    { $count ->
        [one] Parent
       *[other] Parents
    }
# The branches and tags that point at the commit.
details-refs = Refs
details-select-in-log = Select { $commit } in the log
details-copy-hash = Copy the full hash
details-double-click = Double-click for the diff

## The Clone Repository dialog. $host is the name of a hosting service, such as GitHub.

clone-title = Clone Repository
clone-button = Clone
clone-url-label = Repository URL
clone-url-placeholder = https://host/owner/repository.git or git@host:owner/repository.git
clone-url-hint = HTTPS addresses on a server you are signed in to use that sign-in; every other address uses your own Git setup, such as SSH keys and credential helpers.
clone-url-invalid = This is not a Git URL. Use an https, ssh or git address, or user@host:path.
clone-location = Location
clone-folder = Folder
clone-folder-placeholder = folder name
clone-choose-folder = Choose the folder to clone into
clone-target-missing = Choose a location and a folder name.
# $path is the folder the repository would be cloned into.
clone-target-occupied = { $path } exists and is not empty; choose another folder.
clone-target-hint = Clones into { $path }
clone-progress = Cloning into { $path }...
clone-cancelled = The clone was cancelled.
# $name is the repository's name.
task-cloning = Cloning { $name }
clone-failed = { $name } could not be cloned
clone-add-account = Add Account
clone-add-account-tip = Sign in to another account, such as one on another server
# An item of the Add Account menu.
clone-account-of = { $host } Account...
clone-from-account = Clone from your { $host } account
clone-sign-in-hint = Sign in to see the repositories of your account and of the organizations you give Runesmith access to.
sign-in-to = Sign in to { $host }
clone-or-by-url = Or clone any repository by its URL
clone-search = Search repositories by name, owner or description
clone-loading = Loading repositories...
clone-load-failed = The repositories could not be loaded
try-again = Try Again
clone-refresh-tip = Load the repositories again
manage-access = Manage Access
manage-access-tip = Choose which repositories and organizations Runesmith may use, on { $host }
# $account is the name the user is signed in with.
signed-in-as = Signed in to { $host } as { $account }
# The filter that shows the repositories of every owner.
clone-all-owners = All
clone-organization = Organization
clone-add-organization-tip = Let Runesmith use the repositories of another organization on { $host }
clone-add-organization = Add an organization...
clone-no-repositories = No repositories yet
clone-no-repositories-access = The account has no repositories Runesmith may use. Manage Access chooses more.
clone-no-repositories-hint = The account has no repositories yet.
clone-no-match = No repositories match
clone-no-match-hint = Try other words, or show every owner.
# Whose repositories a group of the list holds.
clone-owner-you = Your account
clone-owner-user = User
repository-public = Public
repository-private = Private
repository-fork = Fork
repository-archived = Archived
repository-no-description = No description
# $date is a date and a time.
repository-updated = Updated { $date }

## Pull requests. $host is the name of a hosting service, such as GitHub; $number is a pull request's number.

window-pull-requests = Pull Requests
refresh = Refresh
pr-create = Create Pull Request
# $repository is the repository's name, such as owner/name.
pr-create-description = Ask to merge { $branch } into another branch of { $repository } on { $host }.
pr-base-label = Base: merge into
pr-from-label = From
pr-title-label = Title
pr-description-label = Description
pr-body-placeholder = Describe the change: what it does and why
pr-draft-option = Create as a draft
pr-reading-branches = Reading the branches on { $host }...
pr-push-first-title = Push the branch first?
pr-push-branch-first = { $host } does not have { $branch } yet. Push it to { $remote } first?
# Variables:
#   $count (Number) - how many commits the branch has that the hosting service does not
pr-push-commits-first =
    { $count ->
        [one] { $branch } has 1 commit that { $host } does not have yet. Push it first?
       *[other] { $branch } has { $count } commits that { $host } does not have yet. Push them first?
    }
pr-push-and-create = Push and Create
pr-pushing = Pushing { $branch }...
pr-creating = Creating the pull request...
pr-no-hosted-repository = No hosted repository
pr-no-hosted-repository-create = Open a folder whose Git repository has a remote on a hosting service, such as GitHub, to create a pull request.
pr-no-hosted-repository-hint = Open a folder that is a Git repository with a remote on a hosting service, such as GitHub.
pr-no-remote-hosted = This repository has no remote on a hosting service Runesmith knows.
pr-no-branch = No branch to propose
pr-no-branch-message = HEAD is detached. Switch to a branch, or create one, first.
pr-needs-dialogs = Pull requests need a newer Runesmith
pr-created = Pull request #{ $number } created
open-in-browser = Open in Browser
open-address-in-browser = Open this address in a browser
pr-checking-out = Checking out pull request #{ $number }
pr-switched = Switched to { $branch }
# $title is the pull request's title.
pr-numbered = Pull request #{ $number }: { $title }
pr-checkout-failed = Pull request #{ $number } could not be checked out
pr-repository-on = { $repository } on { $host }
pr-sign-in = Sign in to see pull requests
pr-sign-in-hint = Pull requests, their reviews and checks come from your { $host } account.
pr-none = No open pull requests
pr-none-hint = Pull requests for this repository show here.
pr-load-failed = Pull requests could not be loaded
pr-loading = Loading pull requests...
# $head is the branch with the changes, $target the branch they are merged into.
pr-branch-into = { $head } into { $target }
pr-draft = Draft
pr-approved = Approved
pr-changes-requested = Changes requested
pr-review-required = Review required
pr-checks-passing = Passing
pr-checks-passing-tip = All checks passed
pr-checks-failing = Failing
pr-checks-failing-tip = Some checks failed
pr-checks-running = Running
pr-checks-running-tip = Checks are running
pr-copy-link = Copy Link

## Git itself.

# $command is the Git command, such as push.
# Variables:
#   $code (Number) - the exit code
git-failed = git { $command } failed with exit code { $code }.
# $reason is the system's own message.
git-not-started = Git could not be started: { $reason }. Install Git and make sure it is on the PATH.
