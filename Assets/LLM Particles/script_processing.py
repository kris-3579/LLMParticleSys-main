import os
import json5
import json
from retrying import retry
import re
from openai import OpenAI


prompt_temp_simplify_character_desc = """
Please simplify the character description using one or two simple adjectives.
You should read the input json and respond with a json that has the same format.

Example input:
```json
[
    {'name': 'Tom', 'age': 6, 'gender': 'male'}, 
    {'name': 'Carol', 'age': 45, 'gender': 'female'},
    {'name': 'Max', 'desc': 'Wearing a black t-shirt and jeans, Max is a young male with short, brown hair and blue eyes'},
]
```

Example output:
```json
[
    {'name': 'Tom', 'desc': 'A young boy'}, 
    {'name': 'Carol', 'desc': 'A middle aged woman'},
    {'name': 'Max', 'desc': 'A young male'},
]
```
"""

def simplify_character_desc(character_desc_json: list[dict]) -> str:
    """
    Remove appearance description from character description.
    :param character_desc_json: character descriptions
    :return: simplified character descriptions in json string
    """
    character_desc_json = json.dumps(character_desc_json)

    # query ChatGPT with retry
    response = simplify_with_retry(prompt_temp_simplify_character_desc, character_desc_json)

    return response


@retry(stop_max_attempt_number=3)
def simplify_with_retry(complete_prompt: str, original_script: str) -> str: 
    print("Simplifying character description...")
    json_response = try_extract_content_from_quotes(chat_with_gpt(complete_prompt, original_script))
    json_data = json5.loads(json_response)
    original_json_data = json5.loads(original_script)

    # check if all the names are the same
    original_names = [item['name'] for item in original_json_data]
    new_names = [item['name'] for item in json_data]
    if original_names != new_names:
        raise ValueError("Character description simplifier: the names are not the same")
    
    # check if all entries have the 'desc' key
    for item in json_data:
        if 'desc' not in item:
            raise ValueError("Character description simplifier: not all entries have the 'desc' key")

    return json_response


prompt_temp_audio_desc_decompose = """
You are a sound director that decomposes the an audio description into a composition of several small self-looping audio scripts.
You should read the input JSON and respond with a list in JSON format:

Example input:
```
{
    "desc": "The crackling of the fire and the Village Elder's voice, weaving tales of the forest and its spirits.",
    "people": [{"name": Lily, "desc": a little girl}, {"name": John, "desc": a blacksmith}, {"name": Village Elder, "desc": an old man}]
}
```

Example output:
```json
{
    "audio_scripts": [
        {'type': 'background', 'name': 'fire', 'desc': 'The crackling of the fire in a village', 'duration': 10, 'volume': -35, 'min_pause': 0, 'max_pause': 0},
        {'type': 'foreground', 'name': 'village elder', 'desc': 'An old man is speaking in a village', 'duration': 5, 'volume': -20, 'min_pause': 10, 'max_pause': 30},
    ]
}
```

The meaning of each field in audio scripts is as follows:
- type: either 'background' or 'foreground'. 'background' means the sound is looping seamlessly in the background. 'foreground' means the sound is looping with randomized pause. There should be at least one 'background' sound.
- name: the name of the sound
- desc: the description of the sound. Rewrite the description to replace people's names with its descriptions. For example, replace "John" with "a blacksmith". And describe the sound environment at the end. For example, "in a village".
- duration: the duration of the sound in seconds. Background sounds usually have a duration of 10 seconds. Foreground sounds usually have a duration of 1 ~ 5 seconds.
- volume: the volume of the sound in dB. The volume of background sound is usually around -35 ~ -40 dB. The volume of foreground sound is usually around -20 ~ -30 dB.
- min_pause: for foreground sounds, there should be a pause between each loop. This is the minimum pause in seconds. if the sound only plays once (e.g. the intro music), set this to -1.
- max_pause: this is the maximum pause in seconds. It should be less than 40. if the sound only plays once, set this to -1.

Note:
- Don't use ambiguous words like "forest ambience". Instead, you should break it into specific components like "leaves rustling" and "birds chirping".
- Remove sounds related to narratoring or a single person's speech voice.

"""

def audio_desc_decompose(input_str: str) -> str:
    """
    Remove appearance description from character description.
    """
    # query ChatGPT with retry
    response = audio_desc_decompose_with_retry(prompt_temp_audio_desc_decompose, input_str)
    return response


@retry(stop_max_attempt_number=5)
def audio_desc_decompose_with_retry(complete_prompt: str, input_str: str) -> str: 
    print("Trying to generate audio script...")
    json_response = try_extract_content_from_quotes(chat_with_gpt(complete_prompt, input_str))
    json_data = json5.loads(json_response)

    # check if it contains 'audio_scripts' keys
    if 'audio_scripts' not in json_data:
        raise ValueError("Audio description decomposer: does not have the 'audio_scripts' key")

    # check if there is at least one background sound
    has_background_sound = False
    
    for item in json_data["audio_scripts"]:
        # check if all entries have the 'desc', 'duration', 'volume', 'min_pause', 'max_pause' keys
        if 'desc' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'desc' key")
        if 'duration' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'duration' key")
        if 'volume' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'volume' key")
        if 'min_pause' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'min_pause' key")
        if 'max_pause' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'max_pause' key")
        
        # check if min_pause <= max_pause
        if item['min_pause'] > item['max_pause']:
            raise ValueError("Audio description decomposer: min_pause > max_pause")
        
        # check if max_pause is less than 40
        if item['max_pause'] > 40:
            raise ValueError("Audio description decomposer: max_pause > 40")
        
        # check if volume is in the range of -60 ~ 0
        if item['volume'] < -60 or item['volume'] > 0:
            raise ValueError("Audio description decomposer: volume is not in the range of -60 ~ 0")
        
        # check if duration is positive
        if item['duration'] <= 0:
            raise ValueError("Audio description decomposer: duration is not positive")
        
        # check if type is either 'background' or 'foreground'
        if item['type'] == 'background':
            has_background_sound = True

    # if not has_background_sound:
    #     raise ValueError("Audio description decomposer: there is no background sound")

    return json_response



prompt_sfx_decompose = """
You are a sound director that modify an audio description using a given instruction, then decomposes the new description into a composition of several small audio scripts.
You should read the input JSON and respond with a list in JSON format:

Example input:
```
{
    "description": "The sound of pebbles crunching under Sammy's feet as he walks along the river bank.",
    "people": [{"name": Sammy, "desc": a little boy}, {"name": John, "desc": a blacksmith}, {"name": Village Elder, "desc": an old man}]
}
```

Example output:
```json
{
    "duration": 3,
    "audio_scripts": [
        {'name': 'pebbles', 'desc': 'pebbles crunching at a river bank', 'volume': -20},
    ]
}
```

Example input 2:
```
{
    "description": "The hoot of an owl echoes through the woods, followed by the rustling of leaves.",
    "people": [{"name": Sammy, "desc": a little boy}, {"name": John, "desc": a blacksmith}, {"name": Village Elder, "desc": an old man}]
}
```

Example output 2:
```json
{
    "duration": 5,
    "audio_scripts": [
        {'name': 'owl', 'desc': 'the hoot of an owl at a river bank', 'volume': -20},
        {'name': 'leaves rustling', 'desc': 'the rustling of leaves at a river bank', 'volume': -30},
    ]
}
```

The meaning of each field is as follows:
- duration: the duration of all the sound in seconds, usually around 1 ~ 10.
- name: the name of the sound.
- desc: the description of the sound. Simplify the description by removing text that are unrelated to the sound feature. Rewrite the description to replace people's names with its descriptions. For example, replace "John" with "a blacksmith". And describe the sound environment at the end. For example, "in a village".
- volume: the volume of the sound in dB. The volume of sound effects is usually around -40 ~ -20 dB.

Note:
- Don't use ambiguous words like "forest ambience". Instead, you should break it into specific components like "leaves rustling" and "birds chirping".
- Remove sounds related to narratoring or a single person's speech voice.

"""
def sfx_decompose(input_str: str) -> str:
    # query ChatGPT with retry
    response = sfx_decompose_with_retry(prompt_sfx_decompose, input_str)
    return response


@retry(stop_max_attempt_number=3)
def sfx_decompose_with_retry(complete_prompt: str, input_str: str) -> str: 
    print("Trying to generate audio script...")
    json_response = try_extract_content_from_quotes(chat_with_gpt(complete_prompt, input_str))
    json_data = json5.loads(json_response)

    # check if it contains 'audio_scripts' keys
    if 'audio_scripts' not in json_data:
        raise ValueError("Audio description decomposer: does not have the 'audio_scripts' key")
    # check if it contains 'setting' keys
    if 'duration' not in json_data:
        raise ValueError("Audio description decomposer: does not have the 'duration' key")

    
    for item in json_data["audio_scripts"]:
        # check if all entries have the 'name', 'desc', 'volume' keys
        if 'name' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'name' key")
        if 'desc' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'desc' key")
        if 'volume' not in item:
            raise ValueError("Audio description decomposer: not all entries have the 'volume' key")
        
        # check if volume is in the range of -40 ~ -20
        if item['volume'] < -40 or item['volume'] > -20:
            raise ValueError("Audio description decomposer: volume is not in the range of -40 ~ -20")
        
        # check if duration is around 1 ~ 10
        if json_data['duration'] < 1 or json_data['duration'] > 10:
            raise ValueError("Audio description decomposer: duration is not in the range of 1 ~ 10")

    return json_response



def try_extract_content_from_quotes(content):
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

