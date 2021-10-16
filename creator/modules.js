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
 * Modules 3.0.0 20211016
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211016
 */
import fs from "fs";

import Workspace from "./workspace.js";

export default class Modules {

    static integrate() {
        const modules = Workspace.listVariables()
            .filter(key => key.startsWith("modules.") && Workspace.getVariable(key).match(/^(on|true|yes)$/i))
            .map(key => key.substr(8))
        modules.forEach(module => {
            const moduleFile = Workspace.getModulesDirectory() + "/" + module + "/module.js"
            if (!fs.existsSync(Workspace.getModulesDirectory())
                    || !fs.existsSync(moduleFile))
                return
            console.log("Modules: Integration of " + module)
            // Unfortunately, synchronous dynamic loading of modules does not
            // work, so eval is used here.
            const moduleInstruction = fs.readFileSync(moduleFile).toString()
            const moduleIntegration = eval(moduleInstruction)
            moduleIntegration.apply(this)
        })
    }
}