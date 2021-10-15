/**
 * LIZENZBEDINGUNGEN - Seanox Software Solutions ist ein Open-Source-Projekt, im
 * Folgenden Seanox Software Solutions oder kurz Seanox genannt.
 * Diese Software unterliegt der Version 2 der Apache License.
 *
 * Portable Development Environment
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
 * Workspace 1.0.0 20211015
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 1.0.0 20211015
 */
import fs from "fs"

import Workspace from "./workspace.js"
import Modules from "./modules.js"

export default class Assembler {

    static assemble(yamlFile) {

        // Workspace enable
        console.log("Workspace: Initialization")
        Workspace.initialize(yamlFile)

        // Create a new virtual disk as workspace-drive
        console.log("Drive: Creation and initialization of a new workspace drive")
        Workspace.createDrive()

        // Copying the static structure of the environment
        console.log("Platform: Deployment of static components")
        Workspace.assignDrive()
        Workspace.copyDirectoryInto(Workspace.getPlatformDirectory(), Workspace.getDriveRootDirectory())
        Workspace.detachDrive()

        // Integrates all modules which which are enabled
        console.log("Modules: Integration of the selected modules")
        Workspace.assignDrive()
        Modules.integrate()
        Workspace.detachDrive()

        // Finalization and deployment of the virtual disk in assembly
        // - Defragemntation of the virtual disk
        // - Compacting virtual disk
        // - Deploy virtual hard disk with all scripts in assembly
        console.log("Drive: Finalizing the workspace drive")
        Workspace.finalize()

        const releaseName = Workspace.getVariable("release.name")
        fs.copyFileSync(Workspace.getStartupDirectory() + "/startup.exe", Workspace.getDirectory() + "/" + releaseName + ".exe")
        Workspace.createWorkfile(Workspace.getDriveDirectory() + "/startup.cmd", Workspace.getDirectory() + "/" + releaseName + ".cmd")

        console.log()
        console.log("The Portable Development Environment is completed in:")
        console.log(Workspace.getDirectory())
    }
}