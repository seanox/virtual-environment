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
 * Modules 3.0.0 20211017
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211017
 */
import fs from "fs"
import os from "os"
import path from "path"
import yaml from "yaml"

import Workspace from "./workspace.js"

export default class Modules {

    static integrate() {
        const modules = Workspace.listVariables()
            .filter(key => key.startsWith("modules.") && Workspace.getVariable(key).match(/^(on|true|yes)$/i))
            .map(key => key.substr(8))
        modules.forEach(module => {
            const moduleDirectory = path.normalize(Workspace.getModulesDirectory() + "/" + module)
            const moduleMetaFile = path.normalize(moduleDirectory + "/module.yaml")
            const moduleScriptFile = path.normalize(moduleDirectory + "/module.js")
            if (!fs.existsSync(Workspace.getModulesDirectory())
                    || !fs.existsSync(moduleMetaFile))
                return
            console.log("Modules: Integration of " + module)

            const moduleDestinationDirectory = path.normalize(Workspace.getDestinationModulesDirectory() + "/" + module)

            Workspace.setVariable("module.name", module)
            Workspace.setVariable("module.directory", moduleDirectory)
            Workspace.setVariable("module.destination", moduleDestinationDirectory)

            const moduleMetaWorkFile = Workspace.createWorkfile(moduleMetaFile)

            Workspace.removeVariable("module.name")
            Workspace.removeVariable("module.directory")
            Workspace.removeVariable("module.destination")

            const moduleMetaComplete = yaml.parse(fs.readFileSync(moduleMetaWorkFile).toString())
            if (!moduleMetaComplete
                    || !moduleMetaComplete.module)
                throw new Error("Invalid module meta file: " + moduleMetaFile)
            const moduleMeta = moduleMetaComplete["module"]
            moduleMeta.name = module
            moduleMeta.sourceDirectory = moduleDirectory
            moduleMeta.destinationDirectory = moduleMeta.destination || moduleDestinationDirectory

            if (moduleMeta.download) {
                const moduleDownloadFile = Modules.download(moduleMeta)
                const moduleDownloadDirectory = Workspace.unpackDirectory(moduleDownloadFile)
                Workspace.copyDirectoryInto(moduleDownloadDirectory, moduleMeta.destinationDirectory)
            }

            const profileCommonsFile = Workspace.getDestinationDocumentsProfileDirectory() + "/commons"
            if (moduleMeta.commons)
                fs.appendFileSync(profileCommonsFile, os.EOL + moduleMeta.commons.trim())

            const profileAttachFile = Workspace.getDestinationDocumentsProfileDirectory() + "/attach"
            if (moduleMeta.attach)
                fs.appendFileSync(profileAttachFile, os.EOL + moduleMeta.attach.trim())

            const profileDetachFile = Workspace.getDestinationDocumentsProfileDirectory() + "/detach"
            if (moduleMeta.detach)
                fs.appendFileSync(profileDetachFile, os.EOL + moduleMeta.detach.trim())

            const profileControlFile = Workspace.getDestinationDocumentsProfileDirectory() + "/control"
            if (moduleMeta.control)
                fs.appendFileSync(profileControlFile, os.EOL + moduleMeta.control.trim())

            if (fs.existsSync(moduleScriptFile)) {
                // Unfortunately, synchronous dynamic loading of modules does not
                // work, so eval is used here.
                const moduleInstruction = fs.readFileSync(moduleScriptFile).toString()
                const moduleIntegration = eval(moduleInstruction)
                moduleIntegration.call(this, moduleMeta)
            }
        })
    }

    static download(moduleMeta) {

        // Everything synchronous -- then it doesn't work with the network
        // functions. Because cURL belongs to the Windows board means therefore
        // the workaround.
        const downloadFile = path.normalize(Workspace.getTempDirectory() + "/" + moduleMeta.name + path.extname(moduleMeta.download))
        return Workspace.download(moduleMeta.download, downloadFile)
    }
}