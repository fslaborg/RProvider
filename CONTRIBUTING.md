# Introduction

### Welcome to the project

Thank you for your interest in contributing to RProvider. The project really benefits from wide-ranging experiences and expertise from across the F# and R realms. 

### Please read these guidelines before contributing

As a community project, we (the maintainers and developers) are not paid specifically for working on RProvider. By following these guidelines you help us to keep on top of bug reports, feature requests, code review, and other issues with the limited time we have. In return, we endeavor to provide timely responses and engagement with issues and pull requests. 

### How you can help

There are many ways to contribute to RProvider from large to small.

An simple but impactful way to contribute is to provide tutorials and examples within our documentation. Are you a specialist with a specific set of R packages? Can you show the F# community how to interact with your field or methods?

Other ways to contribute include submitting bug reports, helping trace the causes of bugs, suggesting features, or picking up existing issues and writing code to contribute and address them. 

### What we are NOT looking for

Please avoid submitting questions about how to do something (e.g., how to use a particular R package) into our issue tracker. You can ask on the FsLab discord server, or on Stack Overflow.

Feature requests should fall within the scope of the RProvider project, as defined below. Consider if another library or even a new library may be a better location for an out-of-scope feature request.

# Ground Rules

Please be considerate to others and respect our [code of conduct](https://github.com/fslaborg/RPRovider/blob/master/CODE_OF_CONDUCT.md).

Please be aware of the technical responsibilities below when contributing or merging code:

* RProvider is cross-platform; ensure every change you make retains compatability with macOS, Windows, and linux (supported versions and .NET runtimes).
* Ensure that code reflects the contents of the [F# style guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/). Code should be formatted to closely follow the [code formatting guidelines](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting), and structured to closely follow the [F# component design guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/component-design-guidelines). 
* Create issues for any major changes and enhancements that you wish to make. Discuss things transparently and get community feedback.
* Use the functional paradigm; avoid classes unless absolutely necessary.
* New versions should follow semantic versioning, and should strive to contain only a single new feature at a time.
* Be welcoming to newcomers; encourage and support contributions by providing advice and pointing people in the right direction. 

# Your First Contribution
Issues that may be especially suited to first time contributors are labelled with the [good first issue](https://github.com/fslaborg/RProvider/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22) label on our issue tracker. 

If you're new to making pull requests on open source project, there is a useful tutorial to walk through the process [here](https://github.com/firstcontributions/first-contributions]).

# Getting started
### Submitting a contribution
Make sure there is an open issue relating to your proposed change before getting started.
The process for anything above a typo is:

1. Create your own fork of the code
2. Make the changes in your fork
3. If you are happy with your change and think the project could use it:
    * Be sure you have followed the code style for the project.
    * Be aware of our Code of Conduct.
    * Send a pull request from your fork to our project.

Please bear in mind the following:
* We need all tests to pass. Your fork should build and run all tests successfully using FAKE; use the `dotnet fake run` command to test this.
* We only use GitHub to manage issues. Make any notes or discussion on the relevant issue.

# How to report a bug
If you find a security vulnerability, do NOT open an issue. Send a private message to one of the maintainers listed in README.md instead.

### Filing a bug report
When filing an issue, you can select 'bug report', which will populate your issue with the  template below. Make sure to fill in all sections.

> **Describe the bug**
> A clear and concise description of what the bug is.
> 
> **To Reproduce**
> Steps to reproduce the behavior:
> 1. Go to '...'
> 2. Click on '....'
> 3. Scroll down to '....'
> 4. See error
> 
> **Expected behavior**
> A clear and concise description of what you expected to happen.
> 
> **Screenshots**
> If applicable, add screenshots to help explain your problem.
> 
> **Environment (please complete the following information):**
>  - OS: [e.g. macOS]
>  - OS Version: [e.g. monterey]
>  - OS Architecture: [e.g. Apple silicon, Intel]
>  - Using in script or library: [e.g. FSI, Jupyter notebook, console app]
>  - RProvider Version [e.g. 22]
>  - Installed R Version [e.g. 4.1.1]
> 
> **Additional context**
> Add any other context about the problem here.

# How to suggest a feature or enhancement
### Project goals
The primary goal of RProvider is to provide as-seamless-as-possible interoperability between the R language and F#, making it a first-class solution to use in reproducable scientific research, data science, and statistics. 

RProvider should be a stepping-stone for R users to migrate to F#, and act as a gateway to the wider FsLab and F# ecosystem through interoperability with other common F# libraries. 

### Suggesting a feature
Feature suggestions from the community are the driving force behind the direction of RProvider. If you have an idea for how RProvider could better meet its goals, please consider sharing the idea with the community!

Please open an issue for a 'feature request' in our issue tracker. Make sure to tell us what the feature would achieve, how it fits with the project goals, who may use it, and how it may work. 

# Code review process
### How we accept contributions
One of the maintainers will review your contribution, although this may take up to one working week owing to the other time commitments of the maintainers. 

Contributions will only be signed off once automated checks pass, and a maintainer has then conducted a code review.

If we reply to your issue or pull request and don't hear anything back for two weeks, we may close it. 

# Community
The FsLab discord chat community is a space where you may seek help or discussion outside of GitHub. The link is on our README homepage.

# Code, commit message and labeling conventions

### Preferred code style

Code should be formatted to closely follow the [code formatting guidelines](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting), and structured to closely follow the [F# component design guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/component-design-guidelines). 

### Commit messages

Describe your changes in imperative mood, e.g. "make xyzzy do frotz" instead of "[This patch] makes xyzzy do frotz" or "[I] changed xyzzy to do frotz", as if you are giving orders to the codebase to change its behavior.

Stick to a 50 characrer limit on the first line. If you need to explain further, add after a newline.

### Explain if you use any labeling conventions for issues.

We use a three-part labelling convention for our issues: Type, Status, and Priority. These follow the guide in [sensible-github-labels](https://github.com/Relequestual/sensible-github-labels).

* **Type** indicates whether the issue relates to a bug, feature request, general maintainance tasks, or other things.
* **Status** indicates where the issue is up to. For example, 'Available' issues are ones where someone is yet to take responsibility (e.g. by providing a PR or fix). 'Accepted' issues are where the scope / contents of the issue is clear, and the nature of any resolution has been decided but not completed. 
* **Priority**. We prioritise issues based on their percieved impact on the current user base. 

Only apply one label from each of the three categories to a single issue! Thanks.
