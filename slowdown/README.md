# Slowdown

My computer with i5 3rd generation (i5 3320M) suffers from the updates from
Intel and Windows since Meltdown and Spectre were fixed. When the CPU is
loaded, various IO interfaces have problems, e.g. sound stalls and access to
USB devices.

What helps is to reduce the priority of the processes that generate the load.

With this knowledge I tried numerous prio tools and process managers, but none
convinced me.

Here now another attempt, which convinces at least me :-)

The program does not need to be configured, because the functionality is very
simple. 

If the total CPU usage rises above 25% the program starts to evaluate the CPU
usage of the processes. All processes with a CPU usage are changed in priority
to "IDLE" -- the procedure has worked for me.

If the program is terminated, the original priority is restored.
