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
 * the License
 * .
 * Workspace 1.0.0 20211014
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 1.0.0 20211014
 */
import flat from "flat"
import fs from "fs"
import path from "path"
import yaml from "yaml"

import Diskpart from "./diskpart.js"

const SIGNATURE = new Date().getTime().toString(36).toUpperCase()

const VARIABLES = new Map()

export default class Workspace {

    static getTempDirectory() {
        return Workspace.getVariable("workspace.temp.directory")
    }

    static getPlatformDirectory() {
        return Workspace.getVariable("workspace.platform.directory")
    }

    static getModulesDirectory() {
        return Workspace.getVariable("workspace.modules.directory")
    }

    static getDriveDirectory() {
        return Workspace.getVariable("workspace.drive.directory")
    }

    static getStartupDirectory() {
        return Workspace.getVariable("workspace.startup.directory")
    }

    static getDriveFile() {
        return Workspace.getVariable("workspace.drive.file")
    }

    static initialize(yamlFile) {

        const yamlContent = fs.readFileSync(yamlFile, "utf8")
        const yamlObject = yaml.parse(yamlContent)
        const yamlObjectFlat = flat(yamlObject)
        for (const key in yamlObjectFlat) {
            const value = yamlObjectFlat[key]
            if (typeof value === "function"
                    || typeof value === "object")
                continue;
            Workspace.setVariable(key, value)
        }

        const tempDirectory = path.normalize(path.dirname(yamlFile) + "/temp")
        Workspace.setVariable("workspace.temp.directory", tempDirectory)
        fs.rmdirSync(tempDirectory, {recursive: true})
        fs.mkdirSync(tempDirectory, {recursive: true})

        const workspaceDirectory = path.normalize(path.dirname(yamlFile) + "/workspace")
        Workspace.setVariable("workspace.directory", workspaceDirectory)
        fs.rmdirSync(workspaceDirectory, {recursive: true})
        fs.mkdirSync(workspaceDirectory, {recursive: true})

        const workspaceDriveFile = path.normalize(workspaceDirectory + "/" + Workspace.getVariable("release.name") + ".vhdx")
        Workspace.setVariable("workspace.drive.file", workspaceDriveFile)

        const workspaceStartupDirectory = path.normalize(path.dirname(yamlFile) + "/startup")
        Workspace.setVariable("workspace.startup.directory", workspaceStartupDirectory)

        const workspaceModulesDirectory = path.normalize(path.dirname(yamlFile) + "/modules")
        Workspace.setVariable("workspace.modules.directory", workspaceModulesDirectory)

        const workspacePlatformDirectory = path.normalize(path.dirname(yamlFile) + "/platform")
        Workspace.setVariable("workspace.platform.directory", workspacePlatformDirectory)

        const workspaceDriveDirectory = path.normalize(path.dirname(yamlFile) + "/drive")
        Workspace.setVariable("workspace.drive.directory", workspaceDriveDirectory)

        const workspaceDriveRootDirectory = path.normalize(Workspace.getVariable("workspace.drive") + ":/")
        Workspace.setVariable("workspace.drive.root.directory", workspaceDriveRootDirectory)
    }

    static setVariable(key, value) {
        VARIABLES.set(key, value)
    }

    static getVariable(key) {
        return VARIABLES.get(key)
    }

    static listVariables() {
        return Array.from(VARIABLES.keys())
    }

    static getDirectory() {
        return Workspace.getVariable("workspace.directory")
    }

    static copyDirectoryInto(sourceDir, destinationDir) {
        // TODO:
    }

    static getDriveRootDirectory() {
        return Workspace.getVariable("workspace.drive.root.directory")
    }

    static createDrive() {
        return Diskpart.diskpartExec("diskpart.create")
    }

    static attachDrive() {
        return Diskpart.diskpartExec("diskpart.attach")
    }

    static detachDrive() {
        return Diskpart.diskpartExec("diskpart.detach")
    }

    static finalize() {

        // Compacting, double optimize is effective for virtual drive
        // During the first pass, the data area is optimized, which is
        // effectively used only during the second pass.
        Diskpart.diskpartExec("diskpart.compact")
        Diskpart.diskpartExec("diskpart.compact")
    }

    static createWorkfile(sourceFile) {
        let workFileContent = fs.readFileSync(sourceFile).toString()
        for (const key of Workspace.listVariables())
            workFileContent = workFileContent.replace("#[" + key + "]", Workspace.getVariable(key))
        const workFile = Workspace.getTempDirectory() + "/" + SIGNATURE + "_" + path.basename(sourceFile)
        fs.writeFileSync(workFile, workFileContent)
        return workFile
    }
}