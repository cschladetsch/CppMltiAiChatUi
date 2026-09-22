#!/usr/bin/env python3
"""
Run script for CsharpUAI project
Handles running the various applications in the project
"""

import os
import sys
import subprocess
import argparse
import json
import platform
import shutil
from pathlib import Path

# Platform detection
IS_ANDROID = hasattr(sys, 'getandroidapilevel') or 'ANDROID_ROOT' in os.environ
IS_TERMUX = 'PREFIX' in os.environ and '/com.termux/' in os.environ.get('PREFIX', '')
IS_WINDOWS = platform.system() == 'Windows'
IS_UNIX = platform.system() in ['Linux', 'Darwin']

# Color support detection and setup
def setup_colors():
    """Setup color support with extensive fallbacks"""
    global HAS_COLOR, Fore, Back, Style

    # Check for color support
    color_support = (
        not IS_ANDROID or IS_TERMUX or
        os.getenv('TERM') not in [None, 'dumb'] or
        os.getenv('COLORTERM') is not None or
        os.getenv('FORCE_COLOR') == '1'
    )

    if color_support:
        try:
            from colorama import init, Fore, Back, Style
            init(autoreset=True, convert=IS_WINDOWS, strip=False)
            HAS_COLOR = True
            return
        except ImportError:
            pass

        # Fallback to basic ANSI codes
        try:
            class ANSIColors:
                RED = '\033[31m'
                GREEN = '\033[32m'
                YELLOW = '\033[33m'
                BLUE = '\033[34m'
                MAGENTA = '\033[35m'
                CYAN = '\033[36m'
                WHITE = '\033[37m'
                BRIGHT = '\033[1m'
                RESET_ALL = '\033[0m'

            Fore = Back = Style = ANSIColors()
            HAS_COLOR = True
            return
        except:
            pass

    # No color support
    HAS_COLOR = False
    Fore = Back = Style = type('', (), {'__getattr__': lambda self, name: ''})()
    for attr in ['RED', 'GREEN', 'YELLOW', 'BLUE', 'MAGENTA', 'CYAN', 'WHITE', 'BRIGHT', 'RESET_ALL']:
        setattr(Fore, attr, '')
        setattr(Style, attr, '')

setup_colors()


def print_colored(text, color=None, style=None):
    """Print colored text if colorama is available with encoding fallbacks"""
    try:
        if HAS_COLOR and color:
            color_code = getattr(Fore, color.upper(), '')
            style_code = getattr(Style, style.upper(), '') if style else ''
            print(f"{style_code}{color_code}{text}{Style.RESET_ALL}")
        else:
            print(text)
    except UnicodeEncodeError:
        # Fallback: remove emojis and special characters
        clean_text = text.encode('ascii', 'ignore').decode('ascii')
        if HAS_COLOR and color:
            color_code = getattr(Fore, color.upper(), '')
            style_code = getattr(Style, style.upper(), '') if style else ''
            print(f"{style_code}{color_code}{clean_text}{Style.RESET_ALL}")
        else:
            print(clean_text)
    except Exception:
        # Ultimate fallback
        try:
            print(str(text))
        except:
            print("Output encoding error")


def get_safe_path(path_str):
    """Get platform-safe path"""
    if not path_str:
        return Path.cwd()

    try:
        path = Path(path_str)
        # Handle Android/Termux paths
        if IS_ANDROID or IS_TERMUX:
            # Convert Windows-style paths if needed
            if str(path).startswith('C:\\') or str(path).startswith('c:\\'):
                # Try to map to Android storage
                android_paths = [
                    '/storage/emulated/0/Documents',
                    '/sdcard/Documents',
                    '/data/data/com.termux/files/home',
                    os.path.expanduser('~')
                ]
                for android_path in android_paths:
                    if os.path.exists(android_path):
                        return Path(android_path) / path.name
        return path
    except Exception:
        return Path.cwd()


def find_dotnet_executable():
    """Find dotnet executable with platform-specific fallbacks"""
    dotnet_names = ['dotnet', 'dotnet.exe']

    # Check common paths
    common_paths = [
        '/usr/bin', '/usr/local/bin', '/opt/dotnet',
        '/usr/share/dotnet', '/snap/bin'
    ]

    if IS_ANDROID or IS_TERMUX:
        common_paths.extend([
            '/data/data/com.termux/files/usr/bin',
            os.path.expanduser('~/bin'),
            '/storage/emulated/0/dotnet'
        ])

    # First try shutil.which
    for name in dotnet_names:
        dotnet_path = shutil.which(name)
        if dotnet_path:
            return dotnet_path

    # Then check common paths
    for path_dir in common_paths:
        for name in dotnet_names:
            full_path = os.path.join(path_dir, name)
            if os.path.isfile(full_path) and os.access(full_path, os.X_OK):
                return full_path

    return 'dotnet'  # Fallback to basic name


def run_command(cmd, cwd=None, check=True):
    """Run a command and handle errors with platform-specific adjustments"""
    # Handle command as list vs string
    if isinstance(cmd, list):
        display_cmd = ' '.join(cmd)
        # Replace dotnet with found executable if needed
        if cmd[0] == 'dotnet':
            cmd[0] = find_dotnet_executable()
    else:
        display_cmd = cmd

    print_colored(f"Running: {display_cmd}", "cyan")

    try:
        # Platform-specific subprocess options
        run_kwargs = {
            'cwd': cwd,
            'check': check,
        }

        if isinstance(cmd, str):
            run_kwargs['shell'] = True

        # Android/Termux adjustments
        if IS_ANDROID or IS_TERMUX:
            run_kwargs['timeout'] = 120  # Longer timeout for mobile
            if 'env' not in run_kwargs:
                run_kwargs['env'] = os.environ.copy()
                # Ensure PATH includes Termux paths
                if IS_TERMUX:
                    termux_paths = [
                        '/data/data/com.termux/files/usr/bin',
                        '/data/data/com.termux/files/usr/sbin'
                    ]
                    current_path = run_kwargs['env'].get('PATH', '')
                    for tp in termux_paths:
                        if tp not in current_path:
                            run_kwargs['env']['PATH'] = f"{tp}:{current_path}"

        result = subprocess.run(cmd, **run_kwargs)
        return result

    except subprocess.TimeoutExpired as e:
        print_colored(f"Command timed out: {display_cmd}", "yellow")
        if check:
            sys.exit(1)
        return e
    except subprocess.CalledProcessError as e:
        print_colored(f"Error running command: {e}", "red")
        if check:
            sys.exit(1)
        return e
    except FileNotFoundError as e:
        print_colored(f"Command not found: {display_cmd}", "red")
        if check:
            sys.exit(1)
        return e


def load_config():
    """Load configuration from config.json or config.example.json with fallbacks"""
    # Try multiple config locations
    config_candidates = [
        "MultipleAIApp/config.json",
        "MultipleAIApp/config.example.json",
        "config.json",
        "config.example.json",
        os.path.expanduser("~/CppMltiAiChatUi/MultipleAIApp/config.json"),
        os.path.expanduser("~/config.json")
    ]

    # Add Android-specific paths
    if IS_ANDROID or IS_TERMUX:
        android_configs = [
            "/storage/emulated/0/Documents/config.json",
            "/sdcard/config.json",
            os.path.expanduser("~/storage/shared/config.json")
        ]
        config_candidates.extend(android_configs)

    for config_file in config_candidates:
        config_path = get_safe_path(config_file)
        if config_path.exists():
            try:
                with open(config_path, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    print_colored(f"Loaded config from: {config_path}", "green")
                    return config
            except (json.JSONDecodeError, UnicodeDecodeError) as e:
                print_colored(f"Error loading {config_path}: {e}", "yellow")
            except Exception as e:
                print_colored(f"Failed to read {config_path}: {e}", "yellow")

    print_colored("No config file found, using defaults", "yellow")
    return get_default_config()


def get_default_config():
    """Get default configuration for fallback"""
    return {
        "generation": {
            "pages": 200,
            "loops": 10,
            "deathFrequency": 50,
            "enableGeneration": True,
            "outputDirectory": "generated",
            "colorOutput": True
        },
        "defaultSettings": {
            "maxTokens": 512,
            "temperature": 0.7,
            "timeout": 30000 if not (IS_ANDROID or IS_TERMUX) else 120000,
            "retryAttempts": 3,
            "retryDelay": 1000
        }
    }


def play_adventure(story_file):
    """Play an interactive adventure game"""
    try:
        import json

        # Load story file
        story_path = get_safe_path(story_file)
        if not story_path.exists():
            print_colored(f"❌ Story file not found: {story_path}", "red")
            print_colored("💡 Generate a story first with: python run.py --generate 20", "yellow")
            return False

        with open(story_path, 'r', encoding='utf-8') as f:
            story_data = json.load(f)

        print_colored("🎮 === Choose Your Own Adventure === 🎮", "magenta", "bright")
        print_colored(f"📖 {story_data['title']}", "cyan", "bright")
        print_colored(f"📊 {len(story_data['generatedPages'])} pages available", "green")

        # Initialize game state
        current_page_id = "page_1"
        player_health = story_data['player']['health']
        max_health = story_data['player']['maxHealth']
        inventory = story_data['inventory']['slots']

        print_colored("\n🧙‍♂️ Welcome, adventurer! Your journey begins...\n", "yellow")

        while True:
            # Find current page
            current_page = None
            for page in story_data['generatedPages']:
                if page['id'] == current_page_id:
                    current_page = page
                    break

            if not current_page:
                print_colored("❌ Page not found! Game ending.", "red")
                break

            # Display page
            print_colored("=" * 60, "cyan")
            print_colored(f"📄 {current_page['title']}", "white", "bright")
            print_colored("=" * 60, "cyan")
            print_colored(f"\n{current_page['content']}\n", "white")

            # Show player status
            health_bar = "❤️ " * (player_health // 10) + "🤍" * ((max_health - player_health) // 10)
            print_colored(f"💪 Health: {health_bar} ({player_health}/{max_health})", "green")

            # Show inventory
            print_colored("🎒 Inventory (3x3):", "yellow")
            for row in inventory:
                row_display = ""
                for slot in row:
                    if slot is None:
                        row_display += "[   ] "
                    else:
                        row_display += f"[{slot[:3]:3}] "
                print_colored(f"   {row_display}", "yellow")

            # Handle battle
            if current_page.get('hasBattle', False) and current_page.get('battle'):
                print_colored("\n⚔️  BATTLE ENCOUNTER! ⚔️", "red", "bright")
                battle_data = current_page['battle']
                for enemy in battle_data['enemies']:
                    print_colored(f"🐉 {enemy['name']} appears! (Health: {enemy['health']}, Attack: {enemy['attack']})", "red")

                print_colored("\nBattle options:", "yellow")
                print_colored("1. ⚔️  Attack", "white")
                print_colored("2. 🏃 Flee", "white")
                print_colored("3. 🧪 Use Item (if available)", "white")

                try:
                    battle_choice = input("\n🎯 Choose your action (1-3): ").strip()
                    if battle_choice == "1":
                        damage_dealt = 15  # Simple damage
                        enemy_damage = battle_data['enemies'][0]['attack']
                        print_colored(f"⚔️  You attack for {damage_dealt} damage!", "green")
                        print_colored(f"🩸 Enemy attacks you for {enemy_damage} damage!", "red")
                        player_health = max(0, player_health - enemy_damage)

                        if player_health <= 0:
                            print_colored("💀 You have been defeated! Game Over!", "red", "bright")
                            return True
                        else:
                            print_colored("🎉 You won the battle!", "green", "bright")
                            # Add loot
                            inventory[0][0] = "Sword"  # Simple loot
                            print_colored("📦 You found a Sword!", "yellow")
                    elif battle_choice == "2":
                        print_colored("🏃 You fled from battle!", "yellow")
                    else:
                        print_colored("🧪 No items available!", "yellow")

                except (ValueError, KeyboardInterrupt):
                    print_colored("\n👋 Game interrupted!", "yellow")
                    return True

            # Show choices
            print_colored("\n🛤️  What do you want to do?", "cyan", "bright")
            choices = current_page.get('choices', [])

            if not choices:
                print_colored("🏁 You've reached the end of this adventure!", "green", "bright")
                break

            for i, choice in enumerate(choices, 1):
                print_colored(f"{i}. {choice['text']}", "white")

            print_colored("0. 🚪 Quit game", "red")

            # Get player choice
            try:
                choice_input = input(f"\n🎯 Enter your choice (0-{len(choices)}): ").strip()

                if choice_input == "0":
                    print_colored("👋 Thanks for playing!", "yellow")
                    break

                choice_num = int(choice_input)
                if 1 <= choice_num <= len(choices):
                    selected_choice = choices[choice_num - 1]
                    current_page_id = selected_choice['target']
                    print_colored(f"\n➡️  You chose: {selected_choice['text']}", "green")

                    # Check for loops
                    if current_page.get('hasLoop', False):
                        print_colored("🔄 The story loops back...", "magenta")

                    print_colored("\n" + "="*20 + " CONTINUING " + "="*20, "cyan")
                else:
                    print_colored("❌ Invalid choice! Please try again.", "red")

            except (ValueError, KeyboardInterrupt):
                print_colored("\n👋 Game interrupted!", "yellow")
                break

        print_colored("\n🎮 Game completed! Thanks for playing!", "green", "bright")
        return True

    except FileNotFoundError:
        print_colored(f"❌ Story file not found: {story_file}", "red")
        return False
    except json.JSONDecodeError:
        print_colored("❌ Invalid story file format", "red")
        return False
    except Exception as e:
        print_colored(f"❌ Error playing game: {e}", "red")
        return False


def run_generation(config):
    """Run generation mode with specified parameters"""
    gen_config = config.get("generation", {})
    pages = gen_config.get("pages", 200)
    loops = gen_config.get("loops", 10)
    death_freq = gen_config.get("deathFrequency", 50)
    output_dir = gen_config.get("outputDirectory", "generated")

    print_colored("=== Choose Your Own Adventure Game Generation ===", "magenta", "bright")
    print_colored(f"Platform: {platform.system()}", "cyan")
    if IS_ANDROID:
        print_colored("Android detected", "yellow")
    if IS_TERMUX:
        print_colored("Termux environment", "yellow")

    print_colored(f"Story Pages: {pages}", "green")
    print_colored(f"Story Loops: {loops}", "green")
    print_colored(f"Battle Frequency: {death_freq}", "green")
    print_colored(f"Output Directory: {output_dir}", "green")

    # Create output directory with platform-safe path
    try:
        safe_output_dir = get_safe_path(output_dir)
        safe_output_dir.mkdir(parents=True, exist_ok=True)
        print_colored(f"Output directory created: {safe_output_dir}", "cyan")
    except Exception as e:
        print_colored(f"Warning: Could not create output directory: {e}", "yellow")
        safe_output_dir = Path.cwd()

    print_colored(f"🎮 Starting adventure generation with {pages} pages, {loops} loops...", "yellow")

    try:
        # Generate story structure
        story_data = {
            "title": "Generated Adventure",
            "pages": pages,
            "loops": loops,
            "battleFrequency": death_freq,
            "generatedPages": [],
            "inventory": {"slots": [[None for _ in range(3)] for _ in range(3)]},
            "player": {
                "health": 100,
                "maxHealth": 100,
                "level": 1,
                "experience": 0,
                "stats": {
                    "strength": 10,
                    "dexterity": 10,
                    "intelligence": 10,
                    "constitution": 10,
                    "charisma": 10
                }
            }
        }

        # Adaptive generation for mobile platforms
        batch_size = 5 if (IS_ANDROID or IS_TERMUX) else 10

        for loop in range(loops):
            print_colored(f"📖 Story Loop {loop + 1}/{loops}", "blue")
            pages_this_loop = min(pages // loops, pages - loop * (pages // loops))

            for batch_start in range(0, pages_this_loop, batch_size):
                batch_end = min(batch_start + batch_size, pages_this_loop)

                for page in range(batch_start, batch_end):
                    page_data = {
                        "id": f"page_{page + 1}",
                        "title": f"Adventure Page {page + 1}",
                        "content": f"This is page {page + 1} of your adventure...",
                        "choices": [
                            {"text": "Continue forward", "target": f"page_{page + 2}"},
                            {"text": "Look around", "target": f"page_{page + 1}_explore"}
                        ],
                        "hasBattle": (page + 1) % death_freq == 0,
                        "hasLoop": loop > 0 and page % (pages // loops) == 0
                    }

                    if page_data["hasBattle"]:
                        print_colored(f"  ⚔️  Battle encounter at page {page + 1}", "red")
                        page_data["battle"] = {
                            "enemies": [{"name": "Random Enemy", "health": 30, "attack": 8}]
                        }

                    if page_data["hasLoop"]:
                        print_colored(f"  🔄 Story loop at page {page + 1}", "yellow")
                        page_data["loopTarget"] = f"page_{max(1, page - 10)}"

                    story_data["generatedPages"].append(page_data)
                    print_colored(f"  📄 Generated page {page + 1}...", "cyan")

                # Small delay for mobile platforms to prevent overwhelming
                if IS_ANDROID or IS_TERMUX:
                    import time
                    time.sleep(0.1)

        # Save the generated story
        story_file = safe_output_dir / "adventure_story.json"
        try:
            import json
            with open(story_file, 'w', encoding='utf-8') as f:
                json.dump(story_data, f, indent=2, ensure_ascii=False)
            print_colored(f"📁 Story saved to: {story_file}", "green")
        except Exception as e:
            print_colored(f"⚠️  Could not save story file: {e}", "yellow")

        print_colored("✅ Adventure generation completed!", "green", "bright")
        print_colored(f"🎯 Generated {len(story_data['generatedPages'])} pages with battles and loops", "cyan")

    except KeyboardInterrupt:
        print_colored("\n⚠️  Generation interrupted by user", "yellow")
    except Exception as e:
        print_colored(f"❌ Generation failed: {e}", "red")


def run_application(project_name=None, configuration="Release", build_first=False, generate_mode=False, generate_pages=None, generate_loops=None, generate_death_freq=None):
    """Run the application"""
    if generate_mode:
        config = load_config()

        # Override config with command line arguments
        if generate_pages is not None:
            config.setdefault("generation", {})["pages"] = generate_pages
        if generate_loops is not None:
            config.setdefault("generation", {})["loops"] = generate_loops
        if generate_death_freq is not None:
            config.setdefault("generation", {})["deathFrequency"] = generate_death_freq

        run_generation(config)
        return True

    print_colored("Running application...", "green")

    # Determine which project to run
    runnable_projects = {
        "demo": "MultiLLM.Demo/MultiLLM.Demo.csproj",
        "app": "MultipleAIApp/MultipleAIApp/MultipleAIApp.csproj"
    }

    if project_name and project_name in runnable_projects:
        project_to_run = runnable_projects[project_name]
    else:
        # Default to demo if available, otherwise app
        if os.path.exists(runnable_projects["demo"]):
            project_to_run = runnable_projects["demo"]
            project_name = "demo"
        elif os.path.exists(runnable_projects["app"]):
            project_to_run = runnable_projects["app"]
            project_name = "app"
        else:
            print("No runnable projects found!")
            return False

    if os.path.exists(project_to_run):
        print_colored(f"Running {project_name}: {project_to_run}", "yellow")

        cmd = ["dotnet", "run", "--project", project_to_run]

        # Add configuration
        cmd.extend(["--configuration", configuration])

        # Add no-build flag if not building first
        if not build_first:
            cmd.append("--no-build")

        result = run_command(cmd, check=False)
        return result.returncode == 0
    else:
        print_colored(f"Project {project_to_run} not found!", "red")
        return False


def main():
    """Main run function"""
    parser = argparse.ArgumentParser(description="Run CsharpUAI applications")
    parser.add_argument("project", nargs="?", default="demo",
                       choices=["demo", "app"],
                       help="Project to run (default: demo)")
    parser.add_argument("--configuration", "-c", default="Release",
                       choices=["Debug", "Release"],
                       help="Build configuration (default: Release)")
    parser.add_argument("--build", action="store_true",
                       help="Build before running")

    # Generation arguments
    parser.add_argument("--generate", type=int, metavar="PAGES",
                       help="Enable generation mode with specified number of pages")
    parser.add_argument("--loops", type=int, metavar="N",
                       help="Number of generation loops (default from config)")
    parser.add_argument("--death-frequency", "--death-freq", type=int, metavar="N",
                       help="Death frequency for generation (default from config)")

    # Play mode argument
    parser.add_argument("--play", action="store_true",
                       help="Play an interactive adventure game")
    parser.add_argument("--story", type=str, metavar="FILE",
                       help="Story file to load (default: generated/adventure_story.json)")

    args = parser.parse_args()

    print_colored("=== CsharpUAI Run Script ===", "magenta", "bright")

    # Change to project root directory with platform safety
    try:
        project_root = Path(__file__).parent.resolve()
        if project_root.exists():
            os.chdir(project_root)
            print_colored(f"Working directory: {project_root}", "cyan")
        else:
            print_colored(f"Warning: Project root not found, using current directory", "yellow")
            project_root = Path.cwd()
    except Exception as e:
        print_colored(f"Warning: Could not change directory: {e}", "yellow")
        project_root = Path.cwd()

    try:
        # Check .NET SDK with fallbacks
        dotnet_found = False
        try:
            result = run_command(["dotnet", "--version"], check=False)
            if result.returncode == 0:
                dotnet_found = True
        except:
            pass

        if not dotnet_found:
            if IS_ANDROID or IS_TERMUX:
                print_colored("⚠️  .NET not found - this is expected on Android/Termux", "yellow")
                print_colored("Generation mode will work without .NET", "cyan")
            else:
                print_colored("❌ ERROR: .NET SDK not found. Please install .NET 8 SDK.", "red")
                if not generate_mode:
                    sys.exit(1)

        # Determine if generation mode is enabled
        generate_mode = args.generate is not None
        play_mode = args.play

        if play_mode:
            # Play interactive adventure
            story_file = args.story or "generated/adventure_story.json"
            run_success = play_adventure(story_file)
        else:
            # Run application
            run_success = run_application(
                project_name=args.project,
                configuration=args.configuration,
                build_first=args.build,
                generate_mode=generate_mode,
                generate_pages=args.generate,
                generate_loops=args.loops,
                generate_death_freq=args.death_frequency
            )

        if not run_success:
            print_colored("Failed to run application!", "red")
            sys.exit(1)

    except KeyboardInterrupt:
        print_colored("\nRun interrupted by user.", "yellow")
        sys.exit(1)
    except Exception as e:
        print_colored(f"Run failed: {e}", "red")
        sys.exit(1)


if __name__ == "__main__":
    main()