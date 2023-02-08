# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2023-02-08

- Supporting the new release page format Unity has recently published
- Basic support for multi-word search in release names and notes

## [1.0.2] - 2022-11-06

- Fixed all http GET requests that were failing since a User-Agent header seems to be required on unity3d.com now.

## [1.0.1] - 2022-01-26

- Fixed an OperationCanceledException appearing in the console when closing the NuHub window while a Refresh is running.
- Updated the package manifest for npm

## [1.0.0] - 2022-01-25

Initial submission for package distribution