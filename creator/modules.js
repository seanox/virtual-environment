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
 * Modules 3.0.0 20211105
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211105
 */
import fs from "fs"
import os from "os"
import path from "path"
import yaml from "yaml"

import Workspace from "./workspace.js"

const modulesRegistry = []

export default class Modules {

    static integrate() {

        const parseModuleMetaWorkFile = (moduleMetaFile) => {
            const moduleMetaWorkFile = Workspace.createWorkfile(moduleMetaFile)
            try {return yaml.parse(fs.readFileSync(moduleMetaWorkFile).toString())
            } catch (error) {
                return error
            }
        }

        const parseModuleMetaFile = (module) => {
            const moduleMetaFile = Workspace.getModulesDirectory("/" + module + "/module.yaml")
            const moduleMeta = parseModuleMetaWorkFile(moduleMetaFile)
            if (moduleMeta instanceof Error
                    || !moduleMeta
                    || !moduleMeta.module
                    || typeof moduleMeta.module !== "object") {
                console.log("Modules: Integration of " + module)
                if (moduleMeta instanceof Error)
                    throw moduleMeta
                throw new Error("Invalid module meta file: " + moduleMetaFile)
            }

            const moduleDirectory = Workspace.getModulesDirectory("/" + module)
            if (!fs.existsSync(Workspace.getModulesDirectory())
                    || !fs.existsSync(moduleMetaFile))
                return

            const moduleProgramDirectory = Workspace.getWorkspaceEnvironmentProgramsDirectory("/" + module)
            const moduleEnvironmentProgramDirectory = Workspace.getEnvironmentDirectory(moduleProgramDirectory.substr(3))

            moduleMeta.module.name = module
            moduleMeta.module.directory = moduleDirectory
            if (moduleMeta.module.destination)
                moduleMeta.module.destination = path.normalize(moduleMeta.module.destination)
            else moduleMeta.module.destination = moduleProgramDirectory
            moduleMeta.module.environment = moduleEnvironmentProgramDirectory

            return moduleMeta.module
        }

        const modules = Workspace.listVariables()
            .filter(key => key.startsWith("modules.") && Workspace.getVariable(key).match(/^(on|true|yes)$/i))
            .map(key => parseModuleMetaFile(key.substr(8)))
            .filter(module => !!module)

        Workspace.setVariable("workspace.modules", modules)

        modules.forEach(moduleMeta => {
            if (!moduleMeta.script
                    || !moduleMeta.script.initial
                    || typeof moduleMeta.script.initial !== "string")
                return
            console.log("Modules: Initialization of " + moduleMeta.name)
            const moduleInitialization = eval(moduleMeta.script.initial)
            if (typeof moduleInitialization === "function")
                moduleInitialization.call(this, moduleMeta)
        })

        const integrateModule = (moduleMeta) => {

            const moduleRegitryName = (moduleMeta.name || "").toLowerCase().trim()
            if (!moduleRegitryName
                    || modulesRegistry.includes(moduleRegitryName))
                return
            modulesRegistry.push(moduleRegitryName)

            const moduleDirectory = Workspace.getModulesDirectory("/" + moduleMeta.name)
            const moduleMetaFile = path.normalize(moduleDirectory + "/module.yaml")
            if (!fs.existsSync(Workspace.getModulesDirectory())
                    || !fs.existsSync(moduleMetaFile))
                return

            Workspace.setVariable("module.name", moduleMeta.name)
            Workspace.setVariable("module.directory", moduleMeta.directory)
            Workspace.setVariable("module.destination", moduleMeta.destination)
            Workspace.setVariable("module.environment", moduleMeta.environment)
            Workspace.setVariable("module.meta", moduleMeta.meta)

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

            console.log("Modules: Integration of " + moduleMeta.name)

            if (moduleMeta.download) {
                const moduleDownloadFile = Modules.download(moduleMeta)
                const moduleDownloadDirectory = Workspace.unpackDirectory(moduleDownloadFile)
                Workspace.copyDirectoryInto(moduleMeta.source || moduleDownloadDirectory, moduleMeta.destination)
            }

            if (!moduleMeta.download
                    && moduleMeta.source)
                Workspace.copyDirectoryInto(moduleMeta.source, moduleMeta.destination)

            if (fs.existsSync(moduleDirectory + "/data")
                    && fs.statSync(moduleDirectory + "/data").isDirectory())
                Workspace.copyDirectoryInto(moduleDirectory + "/data", moduleMeta.destination)

            const moduleInstallDirectory = Workspace.getWorkspaceEnvironmentInstallDirectory("/" + moduleMeta.name)
            if (fs.existsSync(moduleDirectory + "/install")
                    && fs.statSync(moduleDirectory + "/install").isDirectory())
                Workspace.copyDirectoryInto(moduleDirectory + "/install", moduleInstallDirectory)

            const workspaceDocumentSettingsDirectory = Workspace.getWorkspaceEnvironmentDocumentsSettingsDirectory()
            if (!fs.existsSync(workspaceDocumentSettingsDirectory))
                fs.mkdirSync(workspaceDocumentSettingsDirectory, {recursive: true})

            const settingsCommonsFile = Workspace.getWorkspaceEnvironmentDocumentsSettingsDirectory("/commons.cmd")
            if (moduleMeta.commons)
                fs.appendFileSync(settingsCommonsFile, os.EOL + moduleMeta.commons.trim())

            const settingsAttachFile = Workspace.getWorkspaceEnvironmentDocumentsSettingsDirectory("/attach.cmd.cmd")
            if (moduleMeta.attach)
                fs.appendFileSync(settingsAttachFile, os.EOL + moduleMeta.attach.trim())

            const settingsDetachFile = Workspace.getWorkspaceEnvironmentDocumentsSettingsDirectory("/detach.cmd")
            if (moduleMeta.detach)
                fs.appendFileSync(settingsDetachFile, os.EOL + moduleMeta.detach.trim())

            // Unfortunately, synchronous dynamic loading of modules does not
            // work, therefore the script from the yaml file is used with eval.
            if (moduleMeta.script) {
                if (typeof moduleMeta.script === "string"
                        && moduleMeta.script.trim()) {
                    const moduleIntegration = eval(moduleMeta.script)
                    if (typeof moduleIntegration === "function")
                        moduleIntegration.call(this, moduleMeta)
                } else if (typeof moduleMeta.script === "object"
                        && typeof moduleMeta.script.immediate === "string") {
                    const moduleIntegration = eval(moduleMeta.script.immediate)
                    if (typeof moduleIntegration === "function")
                        moduleIntegration.call(this, moduleMeta)
                }
            }

            if (moduleMeta.configure) {
                if (!Array.isArray(moduleMeta.configure))
                    moduleMeta.configure = [moduleMeta.configure]
                moduleMeta.configure = moduleMeta.configure
                    .filter(configure => configure && configure.trim())
                    .map(configure => configure.trim())
                moduleMeta.configure.forEach(configureFile => {
                    Workspace.createWorkfile(configureFile, configureFile)
                })
            }

            Workspace.removeVariable("module.name")
            Workspace.removeVariable("module.directory")
            Workspace.removeVariable("module.destination")
            Workspace.removeVariable("module.environment")
            Workspace.removeVariable("module.meta")
        }

        modules.forEach(moduleMeta => {
            integrateModule(moduleMeta)
        })

        modules.forEach(moduleMeta => {
            if (!moduleMeta.script
                    || !moduleMeta.script.final
                    || typeof moduleMeta.script.final !== "string")
                return
            console.log("Modules: Finalization of " + moduleMeta.name)
            const moduleFinalization = eval(moduleMeta.script.final)
            if (typeof moduleFinalization === "function")
                moduleFinalization.call(this, moduleMeta)
        })
    }

    static download(moduleMeta) {

        // Everything synchronous - this reduces the network functions,
        // therefore the workaround with cURL as Windows function.
        const downloadFile = Workspace.getTempDirectory("/" + moduleMeta.name + path.extname(moduleMeta.download))
        return Workspace.download(moduleMeta.download, downloadFile)
    }
}