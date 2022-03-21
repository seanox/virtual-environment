# ShiftDown
My computer with i5 3rd generation (i5 3320M) suffers from the updates from
Intel and Windows since Meltdown and Spectre were fixed. When the CPU is
loaded, various IO interfaces have problems, e.g.  access to USB devices and
sound stutters.

What helps is to reduce the priority of the processes that generate the load.

With this in mind, I tried numerous prio tools and process managers, but none
convinced me.

Here now another attempt, which convinces at least me :-)

The program does not need to be configured, as the functionality is very simple
-- but you can configure it.

There is a threshold (default value is 25%) for the maximum CPU load of the
processes. If any processes exceed this threshold, their priority is first
reduced to Idle and later increased to BelowNormal when the CPU load falls
below the threshold -- the procedure has worked for me.

If the program is ended, the original priority is restored.

__Against to expectations, the program does not make the computer faster, but
tries to improve multitasking so that all programs get enough CPU time, without
application focus and bells and whistles, it's just to improve the work -- but
I also got and considered your wishes.__


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
- Windows 10 or higher
- .NET 4.7.x or higher


# Download
The service is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform/Resources/platform/Settings)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/tree/main/shiftdown/Releases


# Usage
The program is installed as a service, which requires administration
privileges.

```
shiftdown.exe install
shiftdown.exe uninstall
```

To update the program: Stop the service, replace the program file (exe) and
then start the service again.

The service supports start, pause, continue and stop.

```
shiftdown.exe start
shiftdown.exe pause
shiftdown.exe continue
shiftdown.exe stop
```

When the program ends, the priority of the changed processes will be restored.


# Configuration
The program can optionally be configured via the enclosed XML file, which also
describes the details.


# Changes (Change Log)
## 1.3.0 20220313 (summary of the current version)  
BF: Optimization and corrections  
CR: Unification of namespace / platform icon / (sub) project structure  
CR: Added build script/process via ANT  
CR: Optimization of process scanning (reimplementation)  
CR: Optimization of error logging  
CR: Added XML based settings  
CR: Added suspension of the prioritization for freely definable process names  
CR: Added Preventive down prioritization for freely definable process names  
CR: Added expansion of prioritization decrease to processes with the same name  

[Read more](https://raw.githubusercontent.com/seanox/virtual-environment-creator/master/shiftdown/CHANGES)
