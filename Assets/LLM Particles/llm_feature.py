import traceback

from langchain.chat_models import ChatOpenAI
from langchain.prompts import PromptTemplate
from langchain.prompts.chat import ChatPromptTemplate
from langchain.schema.output_parser import StrOutputParser
from retrying import retry

from LLM_AI.llm_utils.base import ChatSequence, Message
from LLM_AI.llm_utils.chat import chat_with_gpt

func_template = """
You are an action-value engineer trying to write action-value functions to solve reinforcement learning tasks as effective as possible.
Your goal is to write an action-value function in python for the environment that will help the agent decide actions in a card game.

# The game
{game_description}

# The policy
In this action-value function, you will focus on the following policy of the game:
{game_policy}

# The input
The function should be able to take a game state and a planned game action as input. The input should be as follows:
{input_description}

# The output
You should return a reward value ranging from 0 to 1. It is an estimate of the probability of winning the game. 
The closer the reward is to 1, the larger chance of winning we will have.
Try to make the output more continuous.
The reward should be calculated based on both the game state and the given game action.

# Response format
You should return a python function in this format:
```python
def score(state: dict, action: str) -> float:
    pass
    return result_score
```
"""

func_refine_template = """
Here are some criteria for the code review:
- No TODOs, pass, placeholders, or any incomplete code;
- Include all code in the score function. Don't create custom class or functions outside;
- the last line should be "return result_score", and the result_score should be a float;
- You can only use the following modules: math, numpy (as np), random;
- no potential bugs;

First, you should check the above criteria one by one and review the code in detail. Show your thinking process.
Then, if the codes are perfect, please end your response with the following sentence:
```
Result is good.
```

Otherwise, you should end your response with the full corrected function code. 
"""

bug_fix_template = """
In the execution of the code, an error occurred:
{error_message}

The 'state' input is:
{state_input}

The 'action' input is:
{action_input}

Please fix the bug and end your response with the full corrected function code.
"""

test_refine_template = """
In the testing of this function, we found the winning rate is {winning_rate:.2f}%, while the winning rate of a random player is {random_winning_rate:.2f}%.

Please refine the function and end your response with the full corrected function code.

You can think in the following ways:
(1) If the winning rate is near random player, then you may rewrite the entire function;
(2) If the winning rate is higher than random player, then you may try re-weighing the parameters/factors in the function;
"""



class LLMFeature:
    code: str = None
    compiled_code = None
    active: bool = True
    game_description: str = None
    game_policy: str = None
    input_description: str = None

    def __init__(self, game_description: str,
                 game_policy: str,
                 input_description: str,
                 code: str = None):
        """
        Given the game description, policy, and input description, generate the code of scoring function.
        :param game_description: describe the game
        :param game_policy: describe the policy
        :param input_description: describe the input
        :param code: the code of this feature, if not provided, the above three parameters are required.
        """
        self.game_description = game_description
        self.game_policy = game_policy
        self.input_description = input_description

        # if code is provided, use the code directly. Otherwise, generate the code.
        if code is not None:
            self.code = code
            return

        print("Generating code for the feature...")
        llm = ChatOpenAI(model_name="gpt-4-1106-preview")
        parser = StrOutputParser()

        func_prompt = ChatPromptTemplate.from_messages([
            ("system", "You are a powerful assistant who designs an AI player for the card game"),
            ("human", func_template),
        ])
        chain1 = func_prompt | llm | parser
        result1 = chain1.invoke({"game_description": game_description,
                                 "game_policy": game_policy,
                                 "input_description": input_description,
                                 })

        func_prompt = func_template.format(game_description=game_description,
                                        game_policy=game_policy,
                                        input_description=input_description,)

        # refine the code using ChatGPT
        chat_seq = ChatSequence()
        chat_seq.append(Message(role="system",
                                content="You are a powerful assistant who designs an AI player for the card game. "))
        chat_seq.append(Message(role="user", content=func_prompt))
        chat_seq.append(Message(role="assistant", content=result1))
        chat_seq.append(Message(role="user", content=func_refine_template))
        result2 = chat_with_gpt(chat_seq)

        # remove delimiters from the result
        code1 = self._sanitize_output(result1)
        code2 = self._sanitize_output(result2)

        # if no code is refined, use the original code
        if code2 != "":
            self.code = code2
            chat_seq.pop()
            chat_seq.pop()
            chat_seq.append(Message(role="assistant", content=result2))
        else:
            self.code = code1
            chat_seq.pop()

        self.chat_history = chat_seq

    def __call__(self, state: dict, action: str) -> float:
        return self.score(state, action)

    def __repr__(self):
        return self.code

    @staticmethod
    def _sanitize_output(text: str) -> str:
        try:
            _, after = text.split("```python")
            result = after.split("```")[0]
            # insert a # before each print() statement
            result = result.replace("print(", "#print(")
            return result
        except ValueError:
            return ""

    @retry(stop_max_attempt_number=3)
    def score(self, state: dict, action: str) -> float:
        if self.code is None:
            raise Exception("No code generated")

        local_vars = {"state": state, "action": action}
        exec_globals = {'math': __import__('math'), 'np': __import__('numpy'), 'random': __import__('random')}

        if self.compiled_code is None:
            # prepare the code
            exec_code = self.code
            exec_code += "\nresult = score(state, action)\n"

            # compile the code
            self.compiled_code = compile(exec_code, "<string>", "exec")

        if not self.active:
            return 0
        
        try:
            exec(self.compiled_code, exec_globals, local_vars)
        except Exception as e:
            print("Error occurred when executing the code. Refining the code...")
            print(f"current policy: {self.game_policy}")
            error_traceback = traceback.format_exc()

            # prompt the gpt to fix the bug
            bug_fix_prompt = bug_fix_template.format(error_message=error_traceback,
                                                    state_input=state,
                                                    action_input=action)
            chat_seq = ChatSequence()
            chat_seq.append(Message(role="system",
                                    content="You are a powerful assistant who designs an AI player for the card game. "))
            func_prompt = func_template.format(game_description=self.game_description,
                                        game_policy=self.game_policy,
                                        input_description=self.input_description)
            chat_seq.append(Message(role="user", content=func_prompt))
            chat_seq.append(Message(role="assistant", content=f"```python\n{self.code}\n```"))
            chat_seq.append(Message(role="user", content=bug_fix_prompt))
            result = chat_with_gpt(chat_seq)
            code = self._sanitize_output(result)

            if code == "":
                print("No code is refined, deactivating the feature...")
                self.active = False
                return 0

            # update the code
            old_code = self.code
            self.code = code

            # print the code, replacing enter with \n
            print("The refined code is:")
            print(self.code.replace("\n", "\\n"))

            # prepare and compile the code again
            exec_code = self.code
            exec_code += "\nresult = score(state, action)\n"
            self.compiled_code = compile(exec_code, "<string>", "exec")

            # execute the code again, if still error, retry the function
            try:
                exec(self.compiled_code, exec_globals, local_vars)
            except Exception as e:
                # rollback the code
                self.code = old_code
                self.compiled_code = None
                raise e

            return local_vars["result"]
        return local_vars["result"]


    def deactivate(self):
        self.active = False
