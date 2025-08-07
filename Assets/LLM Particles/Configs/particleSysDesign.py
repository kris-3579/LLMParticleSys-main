import os
import re
from openai import OpenAI
import json5
import json
from retrying import retry


def particle_sys_design(user_input: str, output_file_path: str):
    # read main prompt from prompts/main.md
    with open("prompts/main.md", "r") as f:
        main_prompt = f.read()

    # read prompts of other submodules
    with open("prompts/shape_mod.md", "r") as f:
        shape_prompt = f.read()
    with open("prompts/color_time_mod.md", "r") as f:
        color_time_prompt = f.read()
    with open("prompts/velocity_time_mod.md", "r") as f:
        velocity_time_prompt = f.read()
    with open("prompts/main_mod.md", "r") as f:
        main_mod_prompt = f.read()
    with open("prompts/emission_mod.md", "r") as f:
        emission_prompt = f.read()
    with open("prompts/noise_mod.md", "r") as f:
        noise_prompt = f.read()
    with open("prompts/size_over_lifetime_mod.md", "r") as f:
        size_over_lifetime_prompt = f.read()
    '''
    with open("prompts/rotation_over_lifetime_mod.md", "r") as f:
        rotation_over_lifetime_prompt = f.read()
    with open("prompts/collision_mod.md", "r") as f:
        collision_prompt = f.read()
    with open("prompts/trails_mod.md", "r") as f:
        trails_prompt = f.read()
    '''
 
 
    key_to_prompt = {
        "MainModule": main_mod_prompt,
        "ShapeModule": shape_prompt,
        "ColorOverLifetimeModule": color_time_prompt,
        "VelocityOverLifetimeModule": velocity_time_prompt,
        "EmissionModule": emission_prompt,
        "NoiseModule": noise_prompt,
        "SizeOverLifetime": size_over_lifetime_prompt  
        
    }
    '''
        
        
        "RotationOverLifetime": rotation_over_lifetime_prompt,
        "Collision": collision_prompt,
        "Trails": trails_prompt
        '''
    

    # decompose the task into subtasks
    json_data = try_get_json_from_gpt(main_prompt, user_input)

    results = {}

    # iterate all elements in the json_data
    for key, value in json_data.items():
        if key in key_to_prompt:
            results[key] = try_get_json_from_gpt(key_to_prompt[key], user_input+": "+value)
        
    # save results to a json file
    with open(output_file_path, "w") as f:
        json.dump(results, f, indent=4)

    return results


@retry(stop_max_attempt_number=5)
def try_get_json_from_gpt(sys_prompt, input_prompt):
    return json5.loads(extract_content_from_quotes(chat_with_gpt(sys_prompt, input_prompt)))

def extract_content_from_quotes(content):
    # extract the content between "```json" and "```"
    match = re.search(r'```json(.*)```', content, re.DOTALL)
    if match:
        return match.group(1).strip()
    else:
        raise ValueError("Character description simplifier: cannot find the json content")
    

def chat_with_gpt(sys_prompt, input_prompt):
    try:
        client = OpenAI(
            # This is the default and can be omitted
            api_key=os.environ.get("OPENAI_API_KEY"),
        )
        response = client.chat.completions.create(
            model="gpt-4-1106-preview",
            messages=[
                {
                    "role": "system",
                    "content": sys_prompt
                },
                {
                    "role": "user",
                    "content": input_prompt
                }
            ],
        )
    except Exception as err:
        print(f'OPENAI ERROR: {err}')
        raise err
    
    return response.choices[0].message.content