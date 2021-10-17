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
 * Workspace 3.0.0 20211017
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211017
 */
import child from "child_process"
import flat from "flat"
import fs from "fs"
import path from "path"
import yaml from "yaml"
import os from "os"

import Diskpart from "./diskpart.js"

const VARIABLES = new Map()

export default class Workspace {

    static getDriveDirectory() {
        return Workspace.getVariable("workspace.drive.directory")
    }

    static getDriveFile() {
        return Workspace.getVariable("workspace.drive.file")
    }

    static getModulesDirectory() {
        return Workspace.getVariable("workspace.modules.directory")
    }

    static getPlatformDirectory() {
        return Workspace.getVariable("workspace.platform.directory")
    }

    static getStartupDirectory() {
        return Workspace.getVariable("workspace.startup.directory")
    }

    static getTempDirectory() {
        return Workspace.getVariable("workspace.temp.directory")
    }

    static initialize(yamlFile) {

        const yamlContent = fs.readFileSync(yamlFile, "utf8")
        const yamlObject = yaml.parse(yamlContent)
        const yamlObjectFlat = flat(yamlObject)
        for (const key in yamlObjectFlat) {
            const value = yamlObjectFlat[key]
            if (typeof value === "function"
                    || typeof value === "object")
                continue
            Workspace.setVariable(key, value)
        }

        const tempDirectory = path.normalize(path.dirname(yamlFile) + "/temp")
        Workspace.setVariable("workspace.temp.directory", tempDirectory)

        const workspaceDirectory = path.normalize(path.dirname(yamlFile) + "/workspace")
        Workspace.setVariable("workspace.directory", workspaceDirectory)

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
        Workspace.setVariable("workspace.destination.directory", workspaceDriveRootDirectory)

        // Detach workspace drives if necessary
        Workspace.detachDrive(false)

        fs.rmSync(tempDirectory, {recursive: true})
        fs.mkdirSync(tempDirectory, {recursive: true})

        fs.rmSync(workspaceDirectory, {recursive: true})
        fs.mkdirSync(workspaceDirectory, {recursive: true})
    }

    static setVariable(key, value) {
        VARIABLES.set(key, value)
    }

    static getVariable(key) {
        return VARIABLES.get(key)
    }

    static removeVariable(key) {
        return VARIABLES.delete(key)
    }

    static listVariables() {
        return Array.from(VARIABLES.keys())
    }

    static getDirectory() {
        return Workspace.getVariable("workspace.directory")
    }

    static copyDirectoryInto(sourceDir, destinationDir) {

        const copy = (copySource, copyDestination) => {

            const stat = fs.statSync(copySource)
            if (!stat || !stat.isDirectory()) {
                if (stat && stat.isFile())
                    fs.copyFileSync(copySource, copyDestination)
                return
            }

            fs.mkdirSync(copyDestination)
            const directory = fs.readdirSync(copySource)
            directory.forEach((file) => {
                const source = path.normalize(copySource + "/" + file)
                const destination = path.normalize(copyDestination + "/" + file)
                copy(source, destination)
            })
        }

        console.log("- copy content from " + sourceDir)
        console.log("  to " + destinationDir)

        if (!fs.existsSync(destinationDir))
            fs.mkdirSync(destinationDir, {recursive: true})
        const directory = fs.readdirSync(sourceDir)
        directory.forEach((file) => {
            const source = path.normalize(sourceDir + "/" + file)
            const destination = path.normalize(destinationDir + "/" + file)
            copy(source, destination)
        })
    }

    static getDestinationDirectory() {
        return Workspace.getVariable("workspace.destination.directory")
    }

    static getDestinationModulesDirectory() {
        return path.normalize(Workspace.getDestinationDirectory() + "/Modules")
    }

    static getDestinationDocumentsDirectory() {
        return path.normalize(Workspace.getDestinationDirectory() + "/Documents")
    }

    static getDestinationDocumentsProfileDirectory() {
        return path.normalize(Workspace.getDestinationDocumentsDirectory() + "/Profile")
    }

    static getProxy() {
        const workspaceProxy = Workspace.getVariable("workspace.proxy")
        if (workspaceProxy.match(/^(off|false|no)$/i))
            return false
        return workspaceProxy
    }

    static createDrive(failure = true) {
        return Diskpart.diskpartExec("diskpart.create", failure)
    }

    static attachDrive(failure = true) {
        return Diskpart.diskpartExec("diskpart.attach", failure)
    }

    static assignDrive(failure = true) {
        Workspace.attachDrive(failure)
        const listDrivesResult = Diskpart.diskpartExec("diskpart.list", failure)
        const listDrivesFilter = new RegExp("^\\s*volume\\s+(\\d+)\\s+(\\w\\s+)?" + Workspace.getVariable("release.display") + "\\s", "im")
        const listDrivesMatch = listDrivesResult.stdout.toString().match(listDrivesFilter)
        if (!listDrivesMatch) {
            console.log(listDrivesResult.stdout.toString())
            throw new Error("Volume for '" + Workspace.getVariable("release.display") + "' was not found in diskpart.list")
        }
        Workspace.setVariable("workspace.drive.number", listDrivesMatch[1])
        return Diskpart.diskpartExec("diskpart.assign", false)
    }

    static detachDrive(failure = true) {
        return Diskpart.diskpartExec("diskpart.detach", failure)
    }

    static finalize() {

        Workspace.assignDrive()
        const defragResult = child.spawnSync("defrag", [Workspace.getVariable("workspace.drive") + ":"])
        if (defragResult instanceof Error)
            throw defragResult
        if (defragResult.status !== 0) {
            console.log(defragResult.stdout)
            throw new Error("An unexpected error occurred during defrag")
        }
        Workspace.detachDrive()

        // Compacting, double optimize is effective for virtual drive
        // During the first pass, the data area is optimized, which is
        // effectively used only during the second pass.
        Diskpart.diskpartExec("diskpart.compact")
        Diskpart.diskpartExec("diskpart.compact")
    }

    static createWorkfile(sourceFile, destinationFile) {

        if (sourceFile === undefined)
            return path.normalize(Workspace.getTempDirectory() + "/" + new Date().getTime().toString(36).toUpperCase())

        let workFileContent = fs.readFileSync(sourceFile).toString()
        for (const key of Workspace.listVariables())
            workFileContent = workFileContent.replace(/(#\[)\s*([^\]]*)\s*(\])/g, (...match) => {
                if (Workspace.listVariables().includes(match[2]))
                    return Workspace.getVariable(match[2])
                return match[0]
            })
        const workFile = path.normalize(destinationFile || Workspace.getTempDirectory() + "/" + path.basename(sourceFile))
        fs.writeFileSync(workFile, workFileContent)
        return workFile
    }

    static download(url, destinationFile) {

        destinationFile = path.normalize(destinationFile || Workspace.getTempDirectory() + "/" + new Date().getTime().toString(36).toUpperCase() + path.extname(url))

        console.log("- download " + url)
        console.log("  to " + destinationFile)

        // Everything synchronous -- then it doesn't work with the network
        // functions. Because cURL belongs to the Windows board means therefore
        // the workaround.

        // Options for cURL
        // -f Fail silently on HTTP errors (with a exit code)
        // -L Follow redirects
        // -o Path of the output file
        // -x URL of the proxy, with empty the option is ignored

        const curlResult = child.spawnSync("curl", [url, "-f", "-L", "-o", destinationFile, "-x", Workspace.getProxy() || ""])
        if (curlResult instanceof Error)
            throw curlResult
        if (curlResult.status === 0)
            return destinationFile
        const errorMessage = curlResult.stderr.toString().match(/curl:\s*\(\s*\d+\s*\)\s*(.*)\s*$/i)
        if (errorMessage)
            throw new Error("An unexpected error occurred during download: " + errorMessage[1])
        console.log(curlResult.stderr.toString())
        throw new Error("An unexpected error occurred during download:" + os.EOL + "\t" + url)
    }

    static unpackDirectory(archiveFile, destinationDirectory) {

        destinationDirectory = path.normalize(destinationDirectory || Workspace.getTempDirectory() + "/" + path.basename(archiveFile).replace(/\..*$/, ""))

        console.log("- unpack " + archiveFile)
        console.log("  to " + destinationDirectory)

        const unpackResult = child.spawnSync(path.normalize(Workspace.getPlatformDirectory() + "/Resources/7zip/7za"), ["x", archiveFile, "-o" + destinationDirectory ])
        if (unpackResult instanceof Error)
            throw unpackResult
        if (unpackResult.status === 0)
            return destinationDirectory
        console.log(unpackResult.stderr.toString())
        throw new Error("An unexpected error occurred during unpack:" + os.EOL + "\t" + archiveFile)
    }
}