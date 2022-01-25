<h1 align="center">
    <a href="https://github.com/Neadrim/com.neadrim.nuhub">
        NuHub
    </a>
</h1>

![](Documentation~/Images/NuHubPreview.png?raw=true)

## Disclaimer

This package is developed for my personal needs and is in no way provided or supported by [Unity Technologies](https://unity.com/).

The code relies entirely on the content available in [Unity download archive](https://unity3d.com/get-unity/download/archive) and may break at any time if Unity decide to change its format. I intend to improve and maintain this package as often as possible, without warranty.

## Introduction

NuHub is a UPM package for Unity Editor that facilitates tracking of Unity releases. It allows to browse release notes and easily install any Editor versions through Unity Hub, starting from Unity 2017.1.0.

Why? I often need to look for specific issues or fixes before updating my projects to a new Unity version and searching through individual web pages in the [Unity Releases Archive](https://unity3d.com/get-unity/download/archive) is tedious. Unfortunately, Unity Hub only provides options to install the latest releases and otherwise redirects you to the [archive page](https://unity3d.com/get-unity/download/archive). I would have liked to see Unity Hub 3.x integrate some of these features but still no luck, so here it is.

## Features

- Browse official Unity releases in a single and organized editor window (Pre-Releases not available yet).
- View available updates based on your current Unity version.
- Search release notes by keywords in multiple releases at once.
- Install any Editor versions in Unity Hub with a single click.
- Automatically check for new releases daily or on demand.

## Requirements

Works with **Unity 2021.2.0** or newer.
I did not bother supporting earlier versions and have no intention to do so since one of my goals with this project was to familiarize myself with [UI Toolkit](https://docs.unity3d.com/2021.2/Documentation/Manual/UIElements.html) but unfortunately, it still had a lot of issues prior to 2021.2.

## Installation

The package is distributed on [GitHub]() and [npm](https://www.npmjs.com/) so it can be added easily to any project using [Unity's Package Manager (UPM)](https://docs.unity3d.com/2021.2/Documentation/Manual/Packages.html).

### Install from Npm

In my opinion, this is the easiest way to install the package and manage version updates in UPM. The package is published to the public unscoped registry which does not require authentication.

1. In Unity, open Project Settings/Package Manager.
2. In the **Scope Registries** section, add the npm registry as follows:
**Name**: npm
**URL**: https://registry.npmjs.org
**Scope(s)**: com.neadrim
*Note that you may add multiple scopes to the list if you are using other packages from npmjs.org.*
![](Documentation~/Images/NpmScopeSettings.png?raw=true)
3. Click "Apply" and wait for unity to refresh the registry.
4. In the [Package Manager window](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-ui.html), select **My Registries** in the **Packages** drop-down menu on the top left.
5. **NuHub** should now appear in the list and be available to install.

For more information, see Unity's documentation about [Installing from a registry](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-ui-install.html).

### Install from Git URL

1. In Unity, open the [Package Manager window](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-ui.html).
2. Click the add **+** button in the top menu bar and select **Add package from Git URL...**
3. Enter [NuHub](https://github.com/Neadrim/com.neadrim.nuhub) Git URL in the text box (https://github.com/Neadrim/com.neadrim.nuhub.git) and click **Add**.
You may also install a specific release, branch or commit by appending it to the URL.
For example: https://github.com/Neadrim/com.neadrim.nuhub.git#1.0.0
![](Documentation~/Images/GitHubSettings.png?raw=true)

For more information, see Unity's documentation about [Installing from a Git URL](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-ui-giturl.html).

### Local / Embedded

Download the latest [release](https://github.com/Neadrim/com.neadrim.nuhub/releases) and follow Unity's guide about [Installing a package from a local folder](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-ui-local.html) or as [embedded dependencies](https://docs.unity3d.com/2021.2/Documentation/Manual/upm-embed.html). In short, simply copy the package content in your project under *./Packages/com.neadrim.nuhub* to embed it.

## Usage

You can open NuHub from the menu **Window/NuHub**.

On the first use, it may take a moment to fetch releases information. You can see the progress in the bottom left status bar. The information is cached in the project Library folder so as long as you don't delete it, subsequent refreshes should be really quick.

By default, NuHub will check for new Unity releases once a day, when the window is open. You can also force it to refresh by clicking the **Refresh** button in the status bar.

## Third-Party Code & Licenses

### [Html Agility Pack](https://github.com/zzzprojects/html-agility-pack)

[The MIT License (MIT)](Editor/lib/htmlagilitypack/LICENSE.md)