# C# NEEO SDK & Drivers

This is a port of the [NEEO SDK](https://github.com/NEEOInc/neeo-sdk) from node.js to modern C# 12 and (cross-platform) .NET 8; that said, the feature set has made the transition but some of the idioms and semantics have changed.  

While I realize this may cause an issue for some developers with extensive knowledge of the JS/TS NEEO SDK, considering the size of that developer base, I'm certain the tradeoffs are worth it for increased code clarity and readability.

## FAQ

### What is NEEO?

*NEEO (the Thinking Remote as it was referred to) is a smart remote control with a cloud based device database, similar in some ways to Logitech Harmony, launched via what was once the biggest kickstarter project - one of the main selling points was that it had a programmable REST API for adding any driver not natively supported.  However in late 2019, NEEO was acquired by Control4 who discontinued it, and at some point soon it is expected that the NEEO cloud will go offline.*

*As such, it behooves those of us who wish to keep using it to define every device via the SDK.*

### Why did you build this? What's wrong with the node.js SDK?

*Technically, nothing is wrong with the node.js SDK. That said, it's not been updated in 6+ years - an eternity in the node ecosystem. It's difficult (though not impossible) to build a newer application using dependencies that don't break with the old versions required for the original SDK (several dependencies have newer non-backwards-compatible versions).*

*The other thing is that I don't get to do C# in my day job and it's my favorite programming environment.*

### I feel like I remember a C# port of the API?

*That wasn't this and this is not a fork of that (or using any of that code) - in fact, I couldn't even find it at first. When I eventually found someone's fork, I realized it was not fully functioning with my NEEO Brain, I'm assuming it had worked and NEEO changed the firmware's API somewhere in the device's lifetime and the author didn't bother to keep up. The NEEO now being a discontinued product, I don't run that same risk.*

### Will you accept PRs?

*Yes, there are certainly places where I could take contributions. Get in touch before spending a lot of time on a feature though.*

### Where are the unit tests?

*I've begun writing tests for the SDK (only) but there is much to be done. You can help by writing some. Open a PR.*

### I found a bug, what should I do?

*Open a github issue. Bonus points if you can fix the bug and offer a PR.*

### Why aren't you using (INSERT RANDOM LIBRARY OR FEATURE HERE)?

*Do I want to? Open a github issue and convice me - perhaps show me an example via a PR (getting the idea yet?)*
