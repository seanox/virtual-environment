/**
 * LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
 * Folgenden Seanox Software Solutions oder kurz Seanox genannt.
 * Diese Software unterliegt der Version 2 der Apache License.
 *
 * Virtual Environment Creator
 * Copyright (C) 2021 Seanox Software Solutions
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * Creator 3.0.0 20211017
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211017
 */
import fs from "fs"

import Modules from "./modules.js"
import Workspace from "./workspace.js"

export default class Creator {

    static assemble(yamlFile) {

        // Workspace enable
        console.log("Workspace: Initialization")
        Workspace.initialize(yamlFile)

        // Create a new virtual disk as workspace-drive
        console.log("Drive: Creation and initialization of a new workspace drive")
        Workspace.createDrive()

        // Copying the static structure of the environment
        // This is done via the Platform module.

        // Integrates all modules which which are enabled
        console.log("Modules: Integration of the selected modules")
        Workspace.assignDrive()
        Modules.integrate()
        Workspace.detachDrive()

        // Finalization and deployment of the virtual disk in workspace
        // - Defragmentation of the virtual disk
        // - Compacting virtual disk
        // - Deploy virtual hard disk with all scripts in workspace
        console.log("Drive: Finalizing the workspace drive")
        Workspace.finalize()

        const environmentName = Workspace.getVariable("environment.name").toLowerCase()
        fs.copyFileSync(Workspace.getStartupDirectory("/startup.exe"), Workspace.getWorkspaceDirectory("/" + environmentName + ".exe"))
        Workspace.createWorkfile(Workspace.getDriveDirectory("/startup.cmd"), Workspace.getWorkspaceDirectory("/" + environmentName + ".cmd"))

        console.log()
        console.log("The virtual environment is completed in:")
        console.log(Workspace.getWorkspaceDirectory())
    }
}