# XoshiroPRNG.Net* Changelog

## 1.0.5 <sup>(as pepoluan.xoshiro)</sup>

* Initial public release
* Targets .Net Framework 4.5.2

## 1.1.0 <sup>(as pepoluan.xoshiro _AND_ XoshiroPRNG.Net)</sup>

* Targets .Net Standard 1.4 _and_ .Net Framework 4.5.2
* Fix on 64-to-32 folding
* Twin release as `pepoluan.xoshiro` and `XoshiroPRNG.Net`

## 1.2.4 <sup>(as pepoluan.xoshiro _AND_ XoshiroPRNG.Net)</sup>

* Additionally, targets .Net Standard 2.0 (as per the guideline)
* Added null checking for some methods
* Fix NextBytes() algorithm
* Remove some pointless interfaces: IRandom and IRandom64
* Some optimizations
* Twin release as `pepoluan.xoshiro` and `XoshiroPRNG.Net`
* Last release for `pepoluan.xoshiro` -- NuGet package and page has been modified to emphasize this.

## 1.2.5 (prerelease)

* LICENSE: Add the "Unlicense" option
* IMPROVE: All concrete classes are sealed for higher performance (proven)
* ADD: Argument validation checks + exceptions
* ADD: Constructor to inject custom PRNG initial state
* ADD: Lots of XML Docs for IntelliSense
* ADD: Explicit Public Domain / PD-like license statement
* ADD: New tests
* CHANGE: Default seed now use DateTime.UtcNow.Ticks
* CHANGE: Internally use nameof() when throwing exceptions
* CHANGE: Method of performing test, now should be easier to just cut&paste from repl.it
* FIX: Comments from original now as comments, not XML Doc
* FIX: XML Docs are now included in the NuGet package
* FIX: Cap the maximum returnable value of Next() to (int.MaxValue - 1), to be 100% compatible with System.Random
* REMOVE: *Base class constructors -- we initialize fields directly now

## ... before 1.5.0

Sorry I forgot to record the changes. I forgot this file exists :sweatsmile:

## 1.5.0

* FIX: Update XoShiRo128** to v1.1 (from upstream)

## 1.6.0

* ADD: GetRandomCompatible() to IRandomU and IRandom64U
