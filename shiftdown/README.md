# ShiftDown

My computer with i5 3rd generation (i5 3320M) suffers from the updates from
Intel and Windows since Meltdown and Spectre were fixed. When the CPU is
loaded, various IO interfaces have problems, e.g.  access to USB devices and
sound stutters.

What helps is to reduce the priority of the processes that generate the load.

With this knowledge I tried numerous prio tools and process managers, but none
convinced me.

Here now another attempt, which convinces at least me :-)

The program does not need to be configured, because the functionality is very
simple. 

If the total CPU usage rises above 25% the program starts to evaluate the CPU
usage of the processes. All processes with a high CPU usage are changed in
priority to "IDLE" -- the procedure has worked for me.

If the program is terminated, the original priority is restored.

__Against the expectation, the program does not reduce the computer's
performance, but tries to improve that all programs get enough CPU time.__


## System Requirement
- Windows 10 or higher as operating system
- .NET 4.7.x or higher


## Download

The service is [part of the virtual environment](https://github.com/seanox/virtual-environment/tree/main/platform/Resources/platform/Settings)
but can also be downloaded and used separately.

https://github.com/seanox/virtual-environment/raw/main/platform/Resources/platform/Settings/shiftdown.exe


## Usage

The program is installed as a service, which requires administration
privileges.

```
sc.exe create ShiftDown binpath="...\shiftdown.exe" start=auto
sc.exe start ShiftDown

sc.exe stop ShiftDown
sc.exe delete ShiftDown
```

The service supports start, pause, continue and stop.  
When the program ends, the priority of the changed processes will be restored.
