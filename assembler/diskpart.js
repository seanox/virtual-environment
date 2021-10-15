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
 * Diskpart 1.0.0 20211015
 * Copyright (C) 2021 Seanox Software Solutions
 * All rights reserved.
 *
 * @author  Seanox Software Solutions
 * @version 1.0.0 20211015
 */
import child from "child_process"

import Workspace from "./workspace.js"

export default class Diskpart {

    static diskpartExec(diskpart, failure = true) {
        const diskpartWorkFile = Workspace.createWorkfile("./assembler/" + diskpart)
        const diskpartResult = child.spawnSync("diskpart", ["/s", diskpartWorkFile])
        if (!failure)
            return diskpartResult
        if (diskpartResult instanceof Error)
            throw diskpartResult
        if (diskpartResult.status === 0)
            return diskpartResult
        if (diskpartResult.output)
            console.log(diskpartResult.stdout.toString())
        throw new Error("An unexpected error occurred during " + diskpart)
    }
}