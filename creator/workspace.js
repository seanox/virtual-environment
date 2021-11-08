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
 * the License
 * .
 * Workspace 3.0.0 20211105
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211105
 */
import child from "child_process"
import flat  from "flat"
import fs    from "fs"
import os    from "os"
import path  from "path"
import yaml  from "yaml"

import Diskpart from "./diskpart.js"

const workspaceVariables = new Map()

const workspaceLocateDirectory = (key, subPath = false) => {
    const directory = Workspace.getVariable(key)
    if (subPath)
        return path.normalize(directory + "/" + subPath)
    return path.normalize(directory)
}

export default class Workspace {

    static getDriveFile() {
        return Workspace.getVariable("workspace.drive.file")
    }

    static getDriveDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.drive.directory", subPath)
    }

    static getModulesDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.modules.directory", subPath)
    }

    static getPlatformDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.platform.directory", subPath)
    }

    static getStartupDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.startup.directory", subPath)
    }

    static getTempDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.temp.directory", subPath)
    }

    static getWorkspaceDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.workspace.directory", subPath)
    }

    static getWorkspaceEnvironmentDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.directory", subPath)
    }

    static getWorkspaceEnvironmentProgramsDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.programs.directory", subPath)
    }

    static getWorkspaceEnvironmentInstallDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.install.directory", subPath)
    }

    static getWorkspaceEnvironmentDatabaseDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.database.directory", subPath)
    }

    static getWorkspaceEnvironmentDocumentsDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.documents.directory", subPath)
    }

    static getWorkspaceEnvironmentResourcesDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.resources.directory", subPath)
    }

    static getWorkspaceEnvironmentTempDirectory(subPath = false) {
        return workspaceLocateDirectory("workspace.environment.temp.directory", subPath)
    }

    static getEnvironmentName() {
        return Workspace.getVariable("environment.name")
    }

    static getEnvironmentDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.directory", subPath)
    }

    static getEnvironmentProgramsDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.programs.directory", subPath)
    }

    static getEnvironmentInstallDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.install.directory", subPath)
    }

    static getEnvironmentDatabaseDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.database.directory", subPath)
    }

    static getEnvironmentDocumentsDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.documents.directory", subPath)
    }

    static getEnvironmentResourcesDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.resources.directory", subPath)
    }

    static getEnvironmentTempDirectory(subPath = false) {
        return workspaceLocateDirectory("environment.temp.directory", subPath)
    }

    //     Important:
    // The module methods are temporarily filled and only usable if
    // a module is integrated, otherwise the methods will cause an error.

    static getModuleName() {
        if (!Workspace.hasVariable("module.name"))
            throw new Error("No active module present")
        return Workspace.getVariable("module.name")
    }

    static getModuleDirectory(subPath = false) {
        if (!Workspace.hasVariable("module.directory"))
            throw new Error("No active module present")
        return workspaceLocateDirectory("module.directory", subPath)
    }

    static getModuleDestinationDirectory(subPath = false) {
        if (!Workspace.hasVariable("module.destination"))
            throw new Error("No active module present")
        return workspaceLocateDirectory("module.destination", subPath)
    }

    static getModuleEnvironmentDirectory(subPath = false) {
        if (!Workspace.hasVariable("module.environment"))
            throw new Error("No active module present")
        return workspaceLocateDirectory("module.environment", subPath)
    }

    static getModuleMeta() {
        if (!Workspace.hasVariable("module.meta"))
            throw new Error("No active module present")
        return Workspace.getVariable("module.meta")
    }

    static getProxy() {
        const workspaceProxy = Workspace.getVariable("workspace.proxy")
        if (workspaceProxy.match(/^(off|false|no)$/i))
            return false
        return workspaceProxy
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

        Workspace.setVariable("workspace.workspace.directory", path.normalize(path.dirname(yamlFile) + "/workspace"))
        Workspace.setVariable("workspace.temp.directory", Workspace.getWorkspaceDirectory("../temp"))
        Workspace.setVariable("workspace.drive.directory", Workspace.getWorkspaceDirectory("../drive"))
        Workspace.setVariable("workspace.drive.file", Workspace.getWorkspaceDirectory("/" + Workspace.getEnvironmentName().toLowerCase() + ".vhdx"))
        Workspace.setVariable("workspace.startup.directory", Workspace.getWorkspaceDirectory("../startup"))
        Workspace.setVariable("workspace.modules.directory", Workspace.getWorkspaceDirectory("../modules"))
        Workspace.setVariable("workspace.platform.directory", Workspace.getWorkspaceDirectory("../platform"))

        Workspace.setVariable("workspace.environment.drive", Workspace.getVariable("workspace.drive"))
        Workspace.setVariable("workspace.environment.directory", Workspace.getVariable("workspace.drive") + ":\\")
        Workspace.setVariable("workspace.environment.database.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.database")))
        Workspace.setVariable("workspace.environment.documents.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.documents")))
        Workspace.setVariable("workspace.environment.install.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.install")))
        Workspace.setVariable("workspace.environment.programs.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.programs")))
        Workspace.setVariable("workspace.environment.resources.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.resources")))
        Workspace.setVariable("workspace.environment.temp.directory", Workspace.getWorkspaceEnvironmentDirectory(Workspace.getVariable("environment.temp")))

        Workspace.setVariable("environment.drive", Workspace.getVariable("environment.drive"))
        Workspace.setVariable("environment.directory", Workspace.getVariable("environment.drive") + ":\\")
        Workspace.setVariable("environment.database.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.database")))
        Workspace.setVariable("environment.documents.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.documents")))
        Workspace.setVariable("environment.install.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.install")))
        Workspace.setVariable("environment.programs.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.programs")))
        Workspace.setVariable("environment.resources.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.resources")))
        Workspace.setVariable("environment.temp.directory", Workspace.getEnvironmentDirectory(Workspace.getVariable("environment.temp")))

        if (fs.existsSync(Workspace.getTempDirectory()))
            fs.rmSync(Workspace.getTempDirectory(), {recursive: true})
        fs.mkdirSync(Workspace.getTempDirectory(), {recursive: true})

        // Detach workspace drives if necessary
        Workspace.detachDrive(false)

        if (fs.existsSync(Workspace.getWorkspaceDirectory()))
            fs.rmSync(Workspace.getWorkspaceDirectory(), {recursive: true})
        fs.mkdirSync(Workspace.getWorkspaceDirectory(), {recursive: true})

        // Node.js may not be able to use files with hidden attributes. That is
        // why the hidden attribute in the platform and module directories is
        // removed recursively. Windows can also set the attribute for system
        // files automatically during cloning from the Git repository.

        Workspace.exec("attrib", ["-H", "-S", "/D", "/S"], {cwd: Workspace.getPlatformDirectory()})
        Workspace.exec("attrib", ["-H", "-S", "/D", "/S"], {cwd: Workspace.getModulesDirectory()})
    }

    static hasVariable(key) {
        return workspaceVariables.has(key)
    }

    static setVariable(key, value) {
        workspaceVariables.set(key, value)
    }

    static getVariable(key) {
        return workspaceVariables.get(key)
    }

    static removeVariable(key) {
        return workspaceVariables.delete(key)
    }

    static listVariables() {
        return Array.from(workspaceVariables.keys())
    }

    static copyDirectoryInto(sourceDir, destinationDir) {

        sourceDir = path.normalize(sourceDir)
        destinationDir = path.normalize(destinationDir)

        const copy = (copySource, copyDestination) => {

            const stat = fs.statSync(copySource)
            if (!stat || !stat.isDirectory()) {
                if (stat && stat.isFile())
                    fs.copyFileSync(copySource, copyDestination)
                return
            }

            if (!fs.existsSync(copyDestination))
                fs.mkdirSync(copyDestination, {recursive: true})
            const directory = fs.readdirSync(copySource)
            directory.forEach(file => {
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
        directory.forEach(file => {
            const source = path.normalize(sourceDir + "/" + file)
            const destination = path.normalize(destinationDir + "/" + file)
            copy(source, destination)
        })
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
        const listDrivesFilter = new RegExp("^\\s*volume\\s+(\\d+)\\s+(\\w\\s+)?" + Workspace.getEnvironmentName() + "\\s", "im")
        const listDrivesMatch = listDrivesResult.stdout.toString().match(listDrivesFilter)
        if (!listDrivesMatch) {
            console.log(listDrivesResult.stdout.toString())
            throw new Error("Volume for '" + Workspace.getEnvironmentName() + "' was not found in diskpart.list")
        }
        Workspace.setVariable("workspace.drive.number", listDrivesMatch[1])
        return Diskpart.diskpartExec("diskpart.assign", false)
    }

    static detachDrive(failure = true) {
        return Diskpart.diskpartExec("diskpart.detach", failure)
    }

    static finalize() {

        Workspace.assignDrive()
        Workspace.exec("defrag", [Workspace.getVariable("workspace.drive") + ":"])
        Workspace.detachDrive()

        // Compacting, double optimize is effective for virtual drive
        // During the first pass, the data area is optimized, which is
        // effectively used only during the second pass.
        Diskpart.diskpartExec("diskpart.compact")
        Diskpart.diskpartExec("diskpart.compact")
    }

    static createWorkfile(sourceFile, destinationFile) {

        if (sourceFile === undefined)
            return Workspace.getTempDirectory("/" + new Date().getTime().toString(36).toUpperCase())

        let workFileContent = fs.readFileSync(sourceFile).toString()
        for (const key of Workspace.listVariables())
            workFileContent = workFileContent.replace(/(#\[)\s*([^\]]*)\s*(\])/g, (...match) => {
                if (Workspace.listVariables().includes(match[2]))
                    return Workspace.getVariable(match[2])
                return match[0]
            })

        if (destinationFile)
            destinationFile = path.normalize(destinationFile)
        else destinationFile = Workspace.getTempDirectory("/" + path.basename(sourceFile))
        fs.writeFileSync(destinationFile, workFileContent)
        return destinationFile
    }

    static download(url, destinationFile) {

        if (destinationFile)
            destinationFile = path.normalize(destinationFile)
        else destinationFile = Workspace.getTempDirectory("/" + new Date().getTime().toString(36).toUpperCase() + path.extname(url))

        const destinationDirectory = path.dirname(destinationFile)
        if (!fs.existsSync(destinationDirectory))
            fs.mkdirSync(destinationDirectory, {recursive: true})

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

        if (destinationDirectory)
            destinationDirectory = path.normalize(destinationDirectory)
        else destinationDirectory = Workspace.getTempDirectory("/" + path.basename(archiveFile).replace(/\..*$/, ""))

        console.log("- unpack " + archiveFile)
        console.log("  to " + destinationDirectory)

        let parameters = [Workspace.getPlatformDirectory("/Resources/7zip/7za"), ["x", archiveFile, "-o" + destinationDirectory]]
        if (path.extname(archiveFile).substr(1).toLowerCase() === "msi")
            parameters = ["msiexec.exe", ["/a", archiveFile, "/qb", "TARGETDIR=" + destinationDirectory]]
        const unpackResult = child.spawnSync(...parameters)
        if (unpackResult instanceof Error)
            throw unpackResult
        if (unpackResult.status === 0)
            return destinationDirectory
        console.log(unpackResult.stderr.toString())
        throw new Error("An unexpected error occurred during unpack:" + os.EOL + "\t" + archiveFile)
    }

    static exec(command, parameters, options) {

        parameters = parameters || []
        options = options || {}

        console.log("- exec " + (command + " " + parameters.join(" ").trim()).trim())
        if (options.cwd)
            console.log("  in " + path.normalize(options.cwd))

        const execResult = child.spawnSync(command, parameters, options)
        if (options.failure === false)
            return execResult
        if (execResult instanceof Error)
            throw execResult
        if (execResult.output
                && (execResult.status !== 0
                        || options.verbose === true))
            console.log(execResult.stdout.toString())
        if (execResult.status === 0)
            return execResult
        console.log(execResult.stderr.toString())
        throw new Error("An unexpected error occurred")
    }
}