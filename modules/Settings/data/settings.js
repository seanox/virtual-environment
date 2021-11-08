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
 * Settings 3.0.0 20211016
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 3.0.0 20211016
 */
import flat from "flat"
import fs   from "fs"
import path from "path"
import yaml from "yaml"

process.on("uncaughtException", (error) => {
    console.error(error.stack || error)
    process.exit(1)
})

if (!process.env.VT_HOME
        || !process.env.VT_NAME)
    process.exit(0)

const settingFile = path.normalize(process.env.VT_HOME + "/" + process.env.VT_NAME.toLowerCase() + ".yaml")
if (!fs.existsSync(settingFile))
    process.exit(0)

const variablesMap = new Map()
for (const key in process.env)
    variablesMap.set("$" + key.toLowerCase(), process.env[key])

const settingFileContent = fs.readFileSync(settingFile).toString().replace(/\$\[(.*?)\]/g,
    (match, variable) => variablesMap.get("$" + variable.toLowerCase()) || "")
const settings = yaml.parse(settingFileContent)
if (!Array.isArray(settings.files))
    process.exit(0)

const settingsFlat = flat(settings.settings || {})
for (const key in settingsFlat) {
    const value = settingsFlat[key]
    if (typeof value === "function"
            || typeof value === "object")
        continue
    variablesMap.set("#" + key.toLowerCase(), value)
}

settings.files.forEach(file => {
    file = path.normalize(file)
    if (fs.existsSync(file)) {
        const fileContent = fs.readFileSync(file).toString().replace(/([\$\#])\[\s*(.*?)\s*\]/g,
            (match, context, variable) => variablesMap.get(context + variable.toLowerCase()) || match)
        fs.writeFileSync(file, fileContent)
    }
})