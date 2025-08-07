#user_input = "large red fireworks with small bursts, with a small gust of wind. the fireworks particles are growing in size over its lifetime."
user_input = "a blue ball growing in size gradually"
output_file_path = "test_output.json"

from dotenv import load_dotenv, find_dotenv

# load environment variables
load_dotenv(find_dotenv())

from particleSysDesign import particle_sys_design

result = particle_sys_design(user_input, output_file_path)
print(result)