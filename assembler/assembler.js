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
 * Workspace 1.0.0 20211014
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 1.0.0 20211014
 */
import Workspace from "./workspace.js"
import Modules from "../modules/modules.js"

export default class Assembler {

    static assemble(yaml) {

        // Workspace enable
        Workspace.initialize(yaml)
        process.on("exit", Workspace.cleanUp.bind(null))

        // Detach workspace drives if necessary
        Workspace.detachDrive()

        // Create a new virtual disk as workspace-drive
        Workspace.createDrive()

        // Copying the static structure of the environment
        Workspace.attachDrive();
        Workspace.copyDirectoryInto("./platform", Workspace.getDrivePath())
        Workspace.detachDrive();

        // Integrates all modules which which are enabled
        Modules.integrate()

        // Finalization and deployment of the virtual disk in assembly
        // - Defragemntation of the virtual disk
        // - Compacting virtual disk
        // - Deploy virtual hard disk with all scripts in assembly
        Workspace.finalize()
    }
}