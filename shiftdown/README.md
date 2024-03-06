<p>
  <!--
  <a href="https://github.com/seanox/process-balancer/pulls">
    <img src="https://img.shields.io/badge/development-active-green?style=for-the-badge">
  </a>
  --> 
  <a href="https://github.com/seanox/process-balancer/pulls"
      title="Development is waiting for new issues / requests / ideas">
    <img src="https://img.shields.io/badge/development-passive-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/seanox/process-balancer/issues">
    <img src="https://img.shields.io/badge/maintenance-active-green?style=for-the-badge">
  </a>
  <a href="http://seanox.de/contact">
    <img src="https://img.shields.io/badge/support-active-green?style=for-the-badge">
  </a>
</p>

__The tool was outsourced to a separate project. Please use
https://github.com/seanox/process-balancer!__



# Process Balancer (formerly ShiftDown)

My computer with i5 3rd generation (i5 3320M) suffers from the updates from
Intel and Windows since Meltdown and Spectre were fixed. When the CPU is loaded,
various IO interfaces have problems, e.g. access to USB devices and sound
stutters.

What helps is to reduce the priority of the processes that generate the load.

With this in mind, I tried numerous prio tools and process managers, but none
convinced me.

Here now another attempt, which convinces at least me :-)

The program does not need to be configured, as the functionality is very simple
-- but you can configure it.

There is a threshold (default value is 25%) for the maximum CPU load of the
processes. If any processes exceed this threshold, their priority is first
reduced to Idle and later increased to BelowNormal when the CPU load falls below
the threshold -- the procedure has worked for me.

If the program is ended, the original priority is restored.

__Against to expectations, the program does not make the computer faster, but
tries to improve multitasking so that all programs get enough CPU time, without
application focus and bells and whistles, it's just to improve the work -- but I
also got and considered your wishes.__


# Features
- Threshold-based two-level down-prioritization of processes
- Expanding down-prioritization to processes with the same name
- Restoration of prioritization when the service is ended
- Optional configuration of processes when prioritization is suspended
- Optional configuration of processes that should always be prioritized down
- Fast analysis and measurement of the cpu load of all processes
- Low additional cpu load due to the service itself
- Includes command line functions to install and uninstall the service, to 
  start, pause, continue and stop the service
- Logging for the Windows Event Viewer


# System Requirement
- Microsoft Windows 10 or higher
- Microsoft .NET 4.8 or higher


# Download
[Seanox Process Balancer 1.3.3](https://github.com/seanox/process-balancer/releases/download/1.3.3/seanox-balancer-1.3.3.zip)  


# Usage
The program is installed as a service, which requires administration privileges.

```
balancer.exe install
balancer.exe uninstall
```

To update the program: Stop the service, replace the program file (exe) and then
start the service again.

The service supports start, pause, continue and stop.

```
balancer.exe start
balancer.exe pause
balancer.exe continue
balancer.exe stop
```

When the program ends, the priority of the changed processes will be restored.


# Configuration
The program can optionally be configured via the enclosed XML file, which also
describes the details.


# Changes
## 1.3.3 20240218  
BF: Review: Optimization and corrections  
CR: Build: Releases are now only available on the release page  
CR: Project: Renaming in Balancer  
CR: Project: Outsourcing as a separate project  
CR: Project: Update TargetFrameworkVersion to v4.8  

[Read more](https://raw.githubusercontent.com/seanox/process-balancer/master/CHANGES)
