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
 * Modules 3.0.0 20211019
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211019
 */
import fs from "fs"
import os from "os"
import path from "path"
import yaml from "yaml"

import Workspace from "./workspace.js"

const REGISTRY = []

export default class Modules {

    static integrate() {

        const modules = Workspace.listVariables()
            .filter(key => key.startsWith("modules.") && Workspace.getVariable(key).match(/^(on|true|yes)$/i))
            .map(key => key.substr(8))

        const integrateModule = (module) => {

            const moduleRegitryName = (module || "").toLowerCase().trim()
            if (!moduleRegitryName
                    || REGISTRY.includes(moduleRegitryName))
                return
            REGISTRY.push(moduleRegitryName)

            const moduleDirectory = Workspace.getModulesDirectory("/" + module)
            const moduleMetaFile = path.normalize(moduleDirectory + "/module.yaml")
            if (!fs.existsSync(Workspace.getModulesDirectory())
                    || !fs.existsSync(moduleMetaFile))
                return

            const moduleProgramDirectory = Workspace.getWorkspaceEnvironmentProgramsDirectory("/" + module)
            const moduleInstallDirectory = Workspace.getWorkspaceEnvironmentInstallDirectory("/" + module)
            const moduleEnvironmentProgramDirectory = Workspace.getEnvironmentDirectory(moduleProgramDirectory.substr(3))

            Workspace.setVariable("module.name", module)
            Workspace.setVariable("module.directory", moduleDirectory)
            Workspace.setVariable("module.destination", moduleProgramDirectory)
            Workspace.setVariable("module.environment", moduleEnvironmentProgramDirectory)

            const moduleMetaWorkFile = Workspace.createWorkfile(moduleMetaFile)

            const parseYaml = (file) => {
                try {return yaml.parse(fs.readFileSync(file).toString())
                } catch (error) {
                    return error
                }
            }

            let moduleMeta = parseYaml(moduleMetaWorkFile)
            if (moduleMeta instanceof Error
                    || !moduleMeta
                    || !moduleMeta.module) {
                console.log("Modules: Integration of " + module)
                if (moduleMeta instanceof Error)
                    throw moduleMeta
                throw new Error("Invalid module meta file: " + moduleMetaFile)
            }

            moduleMeta = moduleMeta["module"]
            moduleMeta.name = module
            moduleMeta.directory = moduleDirectory
            if (moduleMeta.destination)
                moduleMeta.destination = path.normalize(moduleMeta.destination)
            else moduleMeta.destination = moduleProgramDirectory
            moduleMeta.environment = moduleEnvironmentProgramDirectory

            if (moduleMeta.depends) {
                if (!Array.isArray(moduleMeta.depends))
                    moduleMeta.depends = [moduleMeta.depends]
                moduleMeta.depends = moduleMeta.depends
                    .filter(dependence => dependence && dependence.trim())
                    .map(dependence => dependence.trim())
                moduleMeta.depends.forEach(dependence => {
                    integrateModule(dependence)
                })
            }

            console.log("Modules: Integration of " + module)

            if (moduleMeta.download) {
                const moduleDownloadFile = Modules.download(moduleMeta)
                const moduleDownloadDirectory = Workspace.unpackDirectory(moduleDownloadFile)
                Workspace.copyDirectoryInto(moduleMeta.source || moduleDownloadDirectory, moduleMeta.destination)
            }

            if (fs.existsSync(moduleDirectory + "/data")
                    && fs.statSync(moduleDirectory + "/data").isDirectory())
                Workspace.copyDirectoryInto(moduleDirectory + "/data", moduleMeta.destination)

            if (fs.existsSync(moduleDirectory + "/install")
                    && fs.statSync(moduleDirectory + "/install").isDirectory())
                Workspace.copyDirectoryInto(moduleDirectory + "/install", moduleInstallDirectory)

            const profileCommonsFile = Workspace.getWorkspaceEnvironmentDocumentsProfileDirectory("/commons")
            if (moduleMeta.commons)
                fs.appendFileSync(profileCommonsFile, os.EOL + moduleMeta.commons.trim())

            const profileAttachFile = Workspace.getWorkspaceEnvironmentDocumentsProfileDirectory("/attach")
            if (moduleMeta.attach)
                fs.appendFileSync(profileAttachFile, os.EOL + moduleMeta.attach.trim())

            const profileDetachFile = Workspace.getWorkspaceEnvironmentDocumentsProfileDirectory("/detach")
            if (moduleMeta.detach)
                fs.appendFileSync(profileDetachFile, os.EOL + moduleMeta.detach.trim())

            const profileControlFile = Workspace.getWorkspaceEnvironmentDocumentsProfileDirectory("/control")
            if (moduleMeta.control)
                fs.appendFileSync(profileControlFile, os.EOL + moduleMeta.control.trim())

            // Unfortunately, synchronous dynamic loading of modules does not
            // work, therefore the script from the yaml file is used with eval.
            if (moduleMeta.script
                    && moduleMeta.script.trim()) {
                const moduleIntegration = eval(moduleMeta.script)
                if (typeof moduleIntegration === "function")
                    moduleIntegration.call(this, moduleMeta)
            }

            if (moduleMeta.prepare) {
                if (!Array.isArray(moduleMeta.prepare))
                    moduleMeta.prepare = [moduleMeta.prepare]
                moduleMeta.prepare = moduleMeta.prepare
                    .filter(prepare => prepare && prepare.trim())
                    .map(prepare => prepare.trim())
                moduleMeta.prepare.forEach(prepareFile => {
                    Workspace.createWorkfile(prepareFile, prepareFile)
                })
            }

            Workspace.removeVariable("module.name")
            Workspace.removeVariable("module.directory")
            Workspace.removeVariable("module.destination")
            Workspace.removeVariable("module.environment")
        }

        modules.forEach(module => {
            integrateModule(module)
        })
    }

    static download(moduleMeta) {

        // Everything synchronous - this reduces the network functions,
        // therefore the workaround with cURL as Windows function.
        const downloadFile = Workspace.getTempDirectory("/" + moduleMeta.name + path.extname(moduleMeta.download))
        return Workspace.download(moduleMeta.download, downloadFile)
    }
}